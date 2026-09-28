import { Component, ElementRef, ViewChild, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;
  
  productService = inject(ProductService);
  cartService = inject(CartService);
  newProducts: any[] = [];
  isLoading = true;

  ngOnInit() {
    this.productService.getFeaturedProducts().subscribe({
      next: (products) => {
        this.newProducts = products;
        this.isLoading = false;
      },
      error: (err) => {
        console.error("Failed to load featured products", err);
        this.isLoading = false;
      }
    });
  }

  scrollLeft() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: -300, behavior: 'smooth' });
    }
  }

  addToCart(event: Event, product: any) {
    event.preventDefault();
    event.stopPropagation();
    this.cartService.addToCart({ id: product.id, name: product.name, price: product.price, availableQuantity: 99, image: { url: product.imageUrl } } as any, 1);
    alert(product.name + ' ถูกเพิ่มลงตะกร้าแล้ว!');
  }

  scrollRight() {
    if (this.scrollContainer) {
      this.scrollContainer.nativeElement.scrollBy({ left: 300, behavior: 'smooth' });
    }
  }
}
