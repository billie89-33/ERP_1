import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { CartService, CheckoutPayload } from '../../../core/services/cart.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent {
  cartService = inject(CartService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  isCheckoutMode = false;
  isSubmitting = false;
  checkoutForm: FormGroup;

  constructor() {
    this.checkoutForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      address: ['', Validators.required]
    });
  }

  updateQuantity(productId: string, currentQty: number, delta: number) {
    const newQty = currentQty + delta;
    if (newQty > 0) {
      this.cartService.updateQuantity(productId, newQty);
    }
  }

  removeItem(productId: string) {
    this.cartService.removeFromCart(productId);
  }

  startCheckout() {
    this.isCheckoutMode = true;
  }

  cancelCheckout() {
    this.isCheckoutMode = false;
  }

  async submitOrder() {
    if (this.checkoutForm.invalid) {
      this.checkoutForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    try {
      const formValue = this.checkoutForm.value;
      const payload: CheckoutPayload = {
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        email: formValue.email,
        phone: formValue.phone,
        address: formValue.address,
        items: this.cartService.items().map(i => ({
          productId: i.productId,
          quantity: i.quantity
        }))
      };

      const response = await this.cartService.submitCheckout(payload);
      alert(`Checkout successful! Order Number: ${response.orderNumber}`);
      this.router.navigate(['/']); // Redirect to home
    } catch (error: any) {
      alert(`Checkout failed: ${error.error?.message || error.message}`);
    } finally {
      this.isSubmitting = false;
    }
  }
}
