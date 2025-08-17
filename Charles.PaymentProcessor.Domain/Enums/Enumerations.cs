namespace Charles.PaymentProcessor.Domain.Enums;


public enum PaymentMethodType { Card = 1, BankTransfer = 2, MobileMoney = 3 }
public enum PaymentStatus { Pending = 1, Processing = 2, Succeeded = 3, Failed = 4 }