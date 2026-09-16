# Frontend Architecture (Angular)
**Project Type:** Angular SPA (Single Page Application)
**Version:** Angular 18 (Stable, Standalone Components default)
**Language:** TypeScript (TS)
**Paradigm:** Object-Oriented Programming (OOP)

## 🏗️ โครงสร้างการทำงาน (Architecture Flow)
Angular มีการแบ่งแยกหน้าที่การทำงานอย่างชัดเจน (Separation of Concerns) ดังนี้:

### 1. Component (UI & User Interaction)
- **หน้าที่:** จัดการหน้าจอ HTML, รับ Events จากผู้ใช้ (เช่น การกดปุ่มคลิก, การพิมพ์ข้อความ)
- **ข้อควรระวัง:** ไม่ควรเขียน Logic การดึงข้อมูลจาก Database หรือคำนวณที่ซับซ้อนไว้ใน Component

### 2. Service (Data Management & API Calls)
- **หน้าที่:** เป็นตัวกลางคอยวิ่งไปขอข้อมูลจาก Backend (C# API) หรือจัดการ State ตรงกลาง
- **เครื่องมือ:** ใช้ `HttpClient` ของ Angular ในการส่ง HTTP Request (GET, POST, PUT, DELETE)

---

## 💉 Dependency Injection (DI)
แนวคิดหลักที่ทำให้ Angular ทรงพลังคือ DI
แทนที่ Component จะสร้าง Service ขึ้นมาใช้งานเอง Framework จะเป็นคน "ฉีด (Inject)" Service เข้ามาให้ผ่าน **Constructor**

**ตัวอย่างโค้ด:**
```typescript
import { Component, OnInit } from '@angular/core';
import { ProductService } from './product.service';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  products: any[] = [];

  // Angular จะเตรียม ProductService และส่งเข้ามาให้ใช้งานอัตโนมัติ
  constructor(private productService: ProductService) { }

  ngOnInit(): void {
    // Component สั่งให้ Service ไปดึงข้อมูลผ่าน API
    this.productService.getProducts().subscribe(data => {
      this.products = data;
    });
  }
}
```
