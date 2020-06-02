import uuid

from .base import Base
from sqlalchemy import Column
from sqlalchemy.dialects.postgresql import UUID, TEXT, TIMESTAMP


class Customer(Base):
    __tablename__ = "customer"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4, unique=True, nullable=False)
    create_date = Column(TIMESTAMP(False), nullable=False)
    user_info_json = Column(TEXT)


    def __init__(self, user_info_json, create_date):
        """
        Конструктор класса для сущности таблицы Customer
        :param user_info_json: Строка с атрибутами посетителя
        :param create_date: Дата создания
        """
        self.create_date = create_date
        self.user_info_json = user_info_json

