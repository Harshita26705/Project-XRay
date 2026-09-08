export interface PaymentRequest {
  orderId: number;
  amount: number;
  currency: string;
  cardNumber: string;
}

export interface PaymentResult {
  accepted: boolean;
  reason: string;
  transactionId: number;
}

export async function submitPayment(request: PaymentRequest): Promise<PaymentResult> {
  const response = await fetch('/api/payments', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });
  return response.json();
}

export async function getPayment(id: number): Promise<PaymentResult> {
  const response = await fetch(`/api/payments/${id}`);
  return response.json();
}
