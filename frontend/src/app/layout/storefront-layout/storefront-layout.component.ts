import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-storefront-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './storefront-layout.component.html',
  styleUrl: './storefront-layout.component.scss'
})
export class StorefrontLayoutComponent {
  cartService = inject(CartService);
}
