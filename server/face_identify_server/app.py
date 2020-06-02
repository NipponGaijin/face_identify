import models

connection_string = "postgresql://postgres:postgres@localhost:5432/face_identify"

models.CreateDb(connection_string)