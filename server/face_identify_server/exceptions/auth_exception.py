class AuthException(Exception):
    pass


class EmptyHeader(AuthException):
    def __init__(self):
        self.message = "Отсутсвует необходимый заголовок \"Authorization\" с токеном авторизации"


class BadToken(AuthException):
    def __init__(self):
        self.message = "Токен авторизации должен иметь вид \"Bearer <jwt-token>\""


class EmptyUserId(AuthException):
    def __init__(self):
        self.message = "Токен авторизации не содержит информации об идентифицируемом пользователе"


class UserNotFound(AuthException):
    def __init__(self):
        self.message = "Авторизуемый пользователь не найден в системе"
