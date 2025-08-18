# Payment Processor API

A .NET 9 payment processing API that supports payment initialization, webhook handling, and event publishing to AWS SQS. It also includes JWT authentication generated using merchant API keys.

---

## Features

- Initialize payments with unique references and idempotency support
- Handle payment gateway webhooks (idempotent, transactional)
- Publish events to AWS SQS
- JWT authentication using merchant API keys
- Database seeding for merchants and payment methods

---

## Tech Stack

- .NET 9
- Entity Framework Core (PostgreSQL)
- AWS SQS for event publishing
- JWT for authentication
- Moq/XUnit for unit testing

---

## Prerequisites

- .NET 9 SDK
- PostgreSQL
- AWS Account with SQS permissions
- Optional: AWS CLI for local setup

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/Charles-04/Charles.PaymentProcessor.git
cd Charles.PaymentProcessor
```

### 2. Configure environment

Create or update `appsettings.Development.json` with your database, JWT, and AWS configuration.

### 3. Set up AWS Credentials

- Configure a user with programmatic access in AWS IAM.
- Note the Access Key ID and Secret Access Key.
- Set environment variables locally:

```bash
export AWS_ACCESS_KEY_ID=your_access_key
export AWS_SECRET_ACCESS_KEY=your_secret_key
export AWS_REGION=eu-north-1
```

### 4. Run database migrations and seed

```bash
dotnet ef database update
dotnet run
```

On first run, the app will seed merchants and payment methods automatically.

---

## Running the Project

```bash
dotnet run --project Charles.PaymentProcessor.Api
```

- Swagger UI will be available at [http://localhost:5285/swagger](http://localhost:5285/swagger)
- API endpoints:

### Authentication

- **POST** `/auth/apikey`

**Body:**

```json
{
  "apiKey": "merchant_api_key_here"
}
```

**Response:**

```json
{
  "accessToken": "jwt_token",
  "expiresIn": 3600
}
```

### Payments

- **POST** `/api/payments/initialize` (requires JWT)
- **POST** `/api/payments/webhook` (requires webhook secret verification)

---

## AWS SQS Integration

- The API uses AWS SQS to publish events for `payment-initiated` and `payment-completed`.
- The SQS queue URL is retrieved dynamically based on QueueName.
- Ensure the IAM user has `sqs:SendMessage` permission.

---

## Testing

```bash
cd Charles.PaymentProcessor.Test
dotnet test
```

- Unit tests cover payment initialization, webhook handling, and SQS publishing.

---

## Security

- Merchant API keys are hashed using a salt before storage.
- JWT tokens are issued only for valid merchants.
- Webhook payloads are verified using HMAC signatures.

---

## Notes

- **Idempotency:** Each payment has a unique `Reference` and optional `IdempotencyKey` for safe retries.
- **Transactions:** Database operations for initializing payments and processing webhooks are transactional.
- **Local Development:** You can use LocalStack or a mock SQS client for local development if you don’t want to use AWS.

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature-name`)
3. Commit your changes (`git commit -m 'Add new feature'`)
4. Push to the branch (`git push origin feature-name`)
5. Open a pull request

---

## License

MIT License © 2025

