import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { StorefrontLayoutComponent } from './layout/storefront-layout/storefront-layout.component';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // 🔐 Auth Module
  {
    path: 'login',
    component: LoginComponent
  },

  // 🏠 โซน A: Public Storefront (หน้าบ้านลูกค้า)
  {
    path: '',
    component: StorefrontLayoutComponent,
    children: [
      { path: '', loadComponent: () => import('./features/storefront/home/home.component').then(m => m.HomeComponent) },
      { path: 'shop', loadComponent: () => import('./features/storefront/shop/shop.component').then(m => m.ShopComponent) },
      { path: 'product/:id', loadComponent: () => import('./features/storefront/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },
      { path: 'cart', loadComponent: () => import('./features/storefront/cart/cart.component').then(m => m.CartComponent) }
    ]
  },

  // 👨‍💼 โซน B: Admin / ERP Back-Office (หลังบ้านพนักงาน)
  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard], // ใช้ Route Guard ป้องกัน
    children: [
      // { path: 'dashboard', component: DashboardComponent },
      
      // 📦 Product & Inventory
      { path: 'products', loadComponent: () => import('./features/products/product-list/product-list.component').then(m => m.ProductListComponent) },
      { path: 'products/create', loadComponent: () => import('./features/products/product-form/product-form.component').then(m => m.ProductFormComponent) },
      { path: 'products/edit/:id', loadComponent: () => import('./features/products/product-form/product-form.component').then(m => m.ProductFormComponent) },
      
      // 🤝 Sales & CRM
      { 
        path: 'sales-orders', 
        loadComponent: () => import('./features/sales/sales-order-list/sales-order-list.component').then(m => m.SalesOrderListComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Sales'] }
      },
      
      // 🛒 Purchasing
      { 
        path: 'purchase-orders', 
        loadComponent: () => import('./features/purchasing/purchase-order-list/purchase-order-list.component').then(m => m.PurchaseOrderListComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Purchasing'] }
      },
    ]
  },
  
  // Fallback Route
  { path: '**', redirectTo: '' }
];
