import { useState } from 'react';
import { submitPayment } from '../api/paymentApi';
import { placeOrder } from '../api/orderApi';

export default function CheckoutPage() {
  const [status, setStatus] = useState('');

  async function handleCheckout() {
    const order = await placeOrder({ userId: 1, sku: 'SKU-1', amount: 42 });
    const payment = await submitPayment({
      orderId: order.orderId,
      amount: 42,
      currency: 'USD',
      cardNumber: '4111111111111111'
    });
    setStatus(payment.accepted ? 'Payment accepted' : payment.reason);
  }

  return (
    <div>
      <h1>Checkout</h1>
      <button onClick={handleCheckout}>Pay</button>
      <p>{status}</p>
    </div>
  );
}
