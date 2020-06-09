import io
import models
import jwt
import json
import cv2
import numpy as np
import dlib
import uuid
import os

from sklearn import svm
from sklearn.decomposition import PCA
from json import JSONDecodeError
from PIL import Image
from exceptions import AuthException, EmptyHeader, BadToken, EmptyUserId, UserNotFound
from flask import Flask, request, Response
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

connection_string = "postgresql://postgres:postgres@localhost:5432/face_identify"
models.CreateDb(connection_string)
app = Flask(__name__)
engine = create_engine(connection_string, echo=True)
detector = dlib.get_frontal_face_detector()
shape_predictor = dlib.shape_predictor('dlib_models/shape_predictor_68_face_landmarks.dat')
face_rec_model = dlib.face_recognition_model_v1('dlib_models/dlib_face_recognition_resnet_model_v1.dat')
classificator = None

@app.route('/createuser/<username>/<password>', methods=["GET"])
def create_user(username, password):
    session = create_session()

    users = session.query(models.User).filter_by(login=username).all()
    if len(users) > 0:
        session.close()
        return "user {} is exist".format(username), 400
    else:
        session.add(models.User(username, password))
        session.commit()
        session.close()
        return "ok", 200

# Авторизация (получение JWT токена)
@app.route('/auth', methods=["POST"])
def auth():
    body = request.get_json()
    if body["login"] is None:
        return "Параметр \"login\" не может быть пустым", 400
    if body["password"] is None:
        return "Параметр \"password\" не может быть пустым", 400

    session = create_session()

    user = session.query(models.User).filter_by(login=body["login"]).first()
    if user is None:
        session.close()
        return "Пользователь не найден в системе", 400

    check_pass_res = user.check_hash(body['password'])

    user_id = str(user.id)

    if check_pass_res is True:
        encoded_jwt = jwt.encode({
            'id': user_id
        },
        'secret',
        algorithm='HS256'
        )

        ret_body = {
            'token': encoded_jwt.decode("utf-8")
        }

        session.close()
        return Response(json.dumps(ret_body), content_type='application/json; charset=utf-8'), 200
    else:
        session.close()
        return "Неверный пароль", 401

# Идентификация по изображению
@app.route('/identify', methods=["POST"])
def identify():
    try:
        # Проверка авторизации
        check_auth(request)

        # Проверка типа содержимого
        content_type = request.headers.get("Content-Type")

        if content_type.find('multipart/form-data') == -1:
            return "Содержимое запроса должно иметь тип 'multipart/form-data'", 400

        # Получение файла изображения из запроса
        if 'file' not in request.files:
            return "В запросе отсутствует файл изображения лица идентифицируемого человека", 400

        image = request.files['file'].read()
        image = Image.open(io.BytesIO(image))
        image = np.array(image)

        descriptor = get_descriptor(image)
        if descriptor is None:
            return "Не удалось извлечь дескриптор лица человека из изображения"





    except AuthException as ex:
        return ex.message, 401

# Добавление посетителя в БД
@app.route('/add_client', methods=["POST"])
def add_client():
    try:
        # Проверка авторизации
        check_auth(request)

        # Проверка типа содержимого
        content_type = request.headers.get("Content-Type")

        if content_type.find('multipart/form-data') == -1:
            return "Содержимое запроса должно иметь тип 'multipart/form-data'", 400

        # Получение файла изображения из запроса
        if 'file' not in request.files:
            return "В запросе отсутствует файл изображения лица идентифицируемого человека", 400

        image = request.files['file'].read()
        image = Image.open(io.BytesIO(image))
        image = np.array(image)

        descriptor = get_descriptor(image)
        if descriptor is None:
            return "Не удалось извлечь дескриптор лица человека из изображения"

        attributes = request.form["attributes"]
        if attributes is None:
            return "В теле запроса отсутсвуют атрибуты посетителя", 400

        try:
            attributes = json.loads(attributes)
        except JSONDecodeError as ex:
            return "Атрибуты посетителя переданы в некорректном формате: {}".format(ex), 400

        if check_novelty(descriptor) != True:
            return "Пользователь уже известен в системе", 409

        session = create_session()
        customer = models.Customer(json.dumps(attributes))
        session.add(customer)
        session.flush()

        descriptors = models.Descriptors(descriptor, customer.id)
        session.add(descriptors)
        session.flush()

        face_image_id = uuid.uuid4()
        face_image = models.FaceImage(face_image_id, "{0}/{1}.jpg".format(customer.id, face_image_id), customer.id)
        session.add(face_image)
        session.flush()

        os.mkdir("files/{0}".format(customer.id))
        cv2.imwrite("files/{0}/{1}.jpg".format(customer.id, face_image_id), image)

        session.add(customer)

        session.expunge_all()
        session.commit()
        session.close()

        response_data = {
            'userid': str(customer.id)
        }

        return Response(json.dumps(response_data), content_type='application/json; charset=utf-8'), 200
    except AuthException as ex:
        return ex.message, 401

# Проверка авторизации в запросе
def check_auth(request):
    auth_header = request.headers.get("Authorization")
    if auth_header is None:
        raise EmptyHeader()

    auth_header = auth_header.split(" ")
    if len(auth_header) < 2:
        raise BadToken()

    if auth_header[0] != "Bearer":
        raise BadToken()

    decoded_jwt = jwt.decode(auth_header[1], 'secret', algorithms=['HS256'])

    if decoded_jwt['id'] is None:
        raise EmptyUserId()

    userid = decoded_jwt['id']

    session = create_session()
    user = session.query(models.User).filter_by(id=userid).first()
    if user is None:
        session.close()
        raise UserNotFound()
    else:
        session.close()
        return True

def get_descriptor(img):
    """
    Получение дескриптора лица на изображении
    :param img: изображение
    :param shape_predictor: модель для получения ключевых точек лица
    :param face_rec_model: модель для формирования дескриптора
    """

    grayscaled_image = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    detector = dlib.get_frontal_face_detector()
    detected_face = detector(grayscaled_image, 1)

    for i, d in enumerate(detected_face):
        face_shape = shape_predictor(img, d)
        print(face_shape.rect)
        face_descriptor = face_rec_model.compute_face_descriptor(img, face_shape)
        return [i for i in face_descriptor]


# Создание сессии для работы с БД
def create_session():
    Session = sessionmaker()
    Session.configure(bind=engine)
    # session = Session()
    return Session()

# Проверка новизны дескриптора
def check_novelty(descriptor):
    if classificator != None:
        descriptor = np.asarray(descriptor)
        predict_result = classificator.predict(descriptor.reshape(1, -1))
        if predict_result[0] == -1:
            return True
        else:
            return False
    else:
        session = create_session()

        descriptors = session.query(models.Descriptors).all()
        if len(descriptors) > 0:
            customer = None
            dist = 0.6
            for desc in descriptors:
                ed = np.linalg.norm(np.asarray(desc.descriptor) - np.asarray(descriptor))
                if(ed < dist):
                    dist = ed
                    customer = desc
            if customer != None:
                session.close()
                return False
            else:
                session.close()
                return True
        else:
            session.close()
            return True

        session.close()


# Обучение классификатора
def learn_classificator():
    session = create_session()

    descriptors = session.query(models.Descriptors).all()
    if len(descriptors) > 0:
        descriptors = [i.descriptor for i in descriptors]
        descriptors = np.asarray(descriptors, dtype=np.float32)

        global classificator

        if len(descriptors) >= 64:
            pca = PCA(n_components=64)
            descriptors_transformed = pca.fit_transform(descriptors)
            classificator = svm.OneClassSVM(kernel="poly", degree=3, nu=0.1, gamma='auto')
            classificator.fit(descriptors_transformed)
        else:
            classificator = svm.OneClassSVM(kernel="poly", degree=3, nu=0.1, gamma='auto')
            classificator.fit(descriptors)


    session.close()

if __name__ == "__main__":
    # learn_classificator()
    app.run("localhost", debug=False)


