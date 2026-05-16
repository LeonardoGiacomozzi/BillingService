# BillingService - Gestão de Cobrança e Pagamentos

Responsável por gerar orçamentos e processar pagamentos integrados ao Mercado Pago.

## 🛠 Funcionalidades
- Geração automática de orçamentos ao detectar novas ordens.
- Integração com API PIX do Mercado Pago.
- Webhook para recepção de confirmação de pagamento.

## 🏗 Arquitetura

```mermaid
graph LR
    SQS[Amazon SQS] -- "BudgetApproved" --> Billing[Billing Service]
    Billing -- "Save Payment" --> Dynamo[Amazon DynamoDB]
    Billing -- "Process PIX" --> MP[Mercado Pago API]
    MP -- "Notification" --> Webhook[Webhook Controller]
    Webhook --> SQS
```

- **Database**: Amazon DynamoDB (NoSQL).

## 🔄 Fluxo da Saga

O BillingService gerencia a parte financeira da SAGA:

```mermaid
sequenceDiagram
    participant SQS
    participant Billing
    participant MP as Mercado Pago
    SQS->>Billing: Consome OrderOpened
    Billing->>SQS: Publica BudgetCreated
    Note over Billing: Aguarda Aprovação via API
    SQS->>Billing: Consome BudgetApproved
    Billing->>MP: Solicita PIX (Sandbox)
    MP-->>Billing: Notifica Webhook (Pago)
    Billing->>SQS: Publica PaymentProcessed
```
- **Integração**: Mercado Pago SDK.
- **Mensageria**: Amazon SQS.

## 🚀 Pipeline
- Build e Testes Unitários.
- Deploy automático no Amazon EKS.
