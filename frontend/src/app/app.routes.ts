import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { StorefrontLayoutComponent } from './layout/storefront-layout/storefront-layout.component';
import { AdminLayoutComponent } from './layout/admin-layout/admin-layout.component';

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
      // { path: '', component: HomeComponent },
      // { path: 'shop', component: ShopComponent },
      // { path: 'product/:id', component: ProductDetailComponent },
      // { path: 'cart', component: CartComponent },
      // { path: 'checkout', component: CheckoutComponent },
    ]
  },

  // 👨‍💼 โซน B: Admin / ERP Back-Office (หลังบ้านพนักงาน)
  {
    path: 'admin',
    component: AdminLayoutComponent,
    // canActivate: [authGuard], // TODO: ใส่ Route Guard
    children: [
      // { path: 'dashboard', component: DashboardComponent },
      
      // 📦 Product & Inventory
      // { path: 'products', component: ProductListComponent },
      // { path: 'products/create', component: ProductFormComponent },
      // { path: 'products/edit/:id', component: ProductFormComponent },
      // { path: 'categories', component: CategoryListComponent },
      
      // 🤝 Sales & CRM
      // { path: 'customers', component: CustomerListComponent },
      // { path: 'quotations', component: QuotationListComponent },
      // { path: 'sales-orders', component: SalesOrderListComponent },
      
      // 🛒 Purchasing
      // { path: 'suppliers', component: SupplierListComponent },
      // { path: 'purchase-orders', component: PurchaseOrderListComponent },
    ]
  },
  
  // Fallback Route
  { path: '**', redirectTo: '' }
];
