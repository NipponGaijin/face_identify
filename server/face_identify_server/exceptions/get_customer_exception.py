class GetCustomerException(Exception):
    pass

class FindCustomerException(GetCustomerException):
    def __init__(self, message):
        self.message = message