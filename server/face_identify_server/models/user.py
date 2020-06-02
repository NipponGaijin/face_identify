import uuid

from .base import Base
from datetime import datetime
from werkzeug.security import generate_password_hash, check_password_hash
from sqlalchemy import Column
from sqlalchemy.dialects.postgresql import UUID, TEXT, TIMESTAMP


class User(Base):

    __tablename__ = "user"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4, unique=True, nullable=False)
    create_date = Column(TIMESTAMP(False), nullable=False)
    login = Column(TEXT)
    password = Column(TEXT)


    def __init__(self, login, password, create_date):
        """
        Конструктор класса сущности пользователя системы
        :param login:   Логин пользователя
        :param password:    Хэшированный пароль
        :param create_date:     Дата создания записи
        """
        self.login = login
        self.password = generate_password_hash(password)
        self.create_date = datetime.utcnow()



