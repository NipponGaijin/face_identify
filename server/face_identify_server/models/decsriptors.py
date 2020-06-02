import uuid

from datetime import datetime
from .base import Base
from sqlalchemy import Column, ForeignKey
from sqlalchemy.orm import relationship
from sqlalchemy.dialects.postgresql import UUID, DOUBLE_PRECISION, TIMESTAMP, ARRAY

class Descriptors(Base):
    __tablename__ = "descriptors"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4, unique=True, nullable=False)
    create_date = Column(TIMESTAMP(False), nullable=False)
    customer_id = Column(UUID, ForeignKey('customer.id'))
    descriptor = Column(ARRAY(DOUBLE_PRECISION))

    def __init__(self, descriptor, create_date):
        """
        Конструктор класса для таблицы Descriptors
        :param descriptor: Дескриптор лица (массив из 128 double чисел)
        :param create_date: Дата создания записи
        """
        self.descriptor = descriptor
        self.create_date = datetime.utcnow()

