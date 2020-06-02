import models
import jwt
import json

from flask import Flask, request, Response
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

connection_string = "postgresql://postgres:postgres@localhost:5432/face_identify"
models.CreateDb(connection_string)
app = Flask(__name__)
engine = create_engine(connection_string, echo=True)

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


def create_session():
    Session = sessionmaker()
    Session.configure(bind=engine)
    # session = Session()
    return Session()

if __name__ == "__main__":
    app.run("localhost", debug=False)


