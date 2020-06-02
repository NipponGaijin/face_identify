import uuid

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

    def __init__(self, image_path, create_date):
        """
        Конструктор класса для таблицы FaceImage
        :param image_path: Путь к изображению на сервере
        :param create_date: Дата создания изображения
        """
        self.image_path = image_path
        self.create_date = create_date

