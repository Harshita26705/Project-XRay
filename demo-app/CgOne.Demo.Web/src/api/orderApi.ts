export interface OrderRequest {
  userId: number;
  sku: string;
  amount: number;
}

export async function placeOrder(request: OrderRequest): Promise<{ orderId: number }> {
  const response = await fetch('/api/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  });
  return response.json();
}
