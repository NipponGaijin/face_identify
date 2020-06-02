from sqlalchemy import  create_engine
from .base import Base

class CreateDb:
    engine = None

    def __init__(self, connection_string):
        engine = create_engine(connection_string, echo=True)
        Base.metadata.create_all(engine)
