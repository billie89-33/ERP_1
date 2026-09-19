import { Injectable, signal, computed } from '@angular/core';

export interface CartItem {
  productId: string;
  name: string;
  price: number;
  imageUrl: string | null;
  quantity: number;
  maxStock: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  // State
  private cartItemsSignal = signal<CartItem[]>([]);

  // Computed values
  readonly items = this.cartItemsSignal.asReadonly();
  
  readonly totalItems = computed(() => {
    return this.cartItemsSignal().reduce((total, item) => total + item.quantity, 0);
  });
  
  readonly totalPrice = computed(() => {
    return this.cartItemsSignal().reduce((total, item) => total + (item.price * item.quantity), 0);
  });

  constructor() {
    this.loadCart();
  }

  // Actions
  addToCart(product: any, quantity: number = 1) {
    const currentItems = this.cartItemsSignal();
    const existingItem = currentItems.find(item => item.productId === product.id);

    if (existingItem) {
      // Update quantity if already exists, but respect max stock
      const newQty = existingItem.quantity + quantity;
      const finalQty = newQty > product.stockQuantity ? product.stockQuantity : newQty;
      
      this.cartItemsSignal.update(items => 
        items.map(item => 
          item.productId === product.id 
            ? { ...item, quantity: finalQty } 
            : item
        )
      );
    } else {
      // Add new item
      const newItem: CartItem = {
        productId: product.id,
        name: product.name,
        price: product.price,
        imageUrl: product.imageUrl,
        quantity: quantity > product.stockQuantity ? product.stockQuantity : quantity,
        maxStock: product.stockQuantity
      };
      
      this.cartItemsSignal.update(items => [...items, newItem]);
    }
    
    this.saveCart();
  }

  updateQuantity(productId: string, quantity: number) {
    if (quantity <= 0) {
      this.removeFromCart(productId);
      return;
    }

    this.cartItemsSignal.update(items => 
      items.map(item => {
        if (item.productId === productId) {
          const finalQty = quantity > item.maxStock ? item.maxStock : quantity;
          return { ...item, quantity: finalQty };
        }
        return item;
      })
    );
    this.saveCart();
  }

  removeFromCart(productId: string) {
    this.cartItemsSignal.update(items => items.filter(item => item.productId !== productId));
    this.saveCart();
  }

  clearCart() {
    this.cartItemsSignal.set([]);
    this.saveCart();
  }

  // Persistence
  private saveCart() {
    localStorage.setItem('jamine_cart', JSON.stringify(this.cartItemsSignal()));
  }

  private loadCart() {
    const savedCart = localStorage.getItem('jamine_cart');
    if (savedCart) {
      try {
        this.cartItemsSignal.set(JSON.parse(savedCart));
      } catch (e) {
        console.error('Failed to parse cart data from local storage');
      }
    }
  }
}
