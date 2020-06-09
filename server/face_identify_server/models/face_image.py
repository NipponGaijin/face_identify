import uuid

from datetime import datetime
from .base import Base
from sqlalchemy import Column, ForeignKey
from sqlalchemy.orm import relationship
from sqlalchemy.dialects.postgresql import UUID, TEXT, TIMESTAMP

class FaceImage(Base):
    __tablename__ = "face_image"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4, unique=True, nullable=False)
    create_date = Column(TIMESTAMP(False), nullable=False)
    customer_id = Column(UUID, ForeignKey('customer.id'))
    image_path = Column(TEXT)

    def __init__(self, id, image_path, customer_id):
        """
        Конструктор класса для таблицы FaceImage
        :param image_path: Путь к изображению на сервере
        :param customer_id: Идентификатор посетителя
        """
        self.id = str(id)
        self.image_path = image_path
        self.create_date = datetime.utcnow()
        self.customer_id = str(customer_id)

