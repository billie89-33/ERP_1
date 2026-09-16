# โครงสร้างโฟลเดอร์ (Folder Structure)

เพื่อความเป็นระเบียบและรองรับการขยายตัว (Scalability) เราจะจัดโครงสร้างของ C# เป็นแบบคล้าย MVC (เน้น API) และจัด Angular เป็นแบบ Feature-Modular ครับ

---

## ⬛ 1. Backend (C# .NET 8 Web API)
แบ่งโฟลเดอร์ตามหน้าที่การทำงาน (Layered Architecture) เพื่อให้จัดการง่ายและแยกส่วนกันชัดเจน:

```text
E:\Jamine_ERP\backend\
 ├── Controllers/      # ประตูรับ Request จาก Angular (เช่น ProductController.cs)
 ├── DTOs/             # 📌 พระเอกของเรา! ไฟล์คัดกรองข้อมูล (เช่น ProductCreateDto.cs, LoginRequest.cs)
 ├── Models/           # โครงสร้างตาราง Database ของ EF Core (เช่น Product.cs, User.cs)
 ├── Services/         # สมองของระบบ (Business Logic) คอยคำนวณและประมวลผล (เช่น ProductService.cs)
 ├── Data/             # ที่อยู่ของไฟล์ DbContext สำหรับต่อ Database (Supabase)
 ├── Migrations/       # โฟลเดอร์ที่ EF Core สร้างให้อัตโนมัติ (เก็บประวัติการสร้างตาราง)
 ├── Program.cs        # ไฟล์ตั้งค่าเริ่มต้นของระบบ (เปิดใช้ Swagger, Middleware)
 └── appsettings.json  # ไฟล์เก็บความลับ เช่น Connection String และรหัส JWT Secret
```

---

## 🅰️ 2. Frontend (Angular 18)
ถึงแม้ Angular 18 จะใช้ Standalone Components แต่เราจะจัดกลุ่มโฟลเดอร์เป็นแบบ **Modular (Feature-based)** เพื่อให้คนในทีมหาไฟล์เจอง่ายครับ:

```text
E:\Jamine_ERP\frontend\src\app\
 ├── core/             # แกนหลักของแอป (โหลดครั้งเดียว): 
 │    ├── guards/      # ตัวบล็อกหน้าจอ (เช่น บล็อกเซลส์ไม่ให้เข้าหน้าจัดซื้อ)
 │    ├── interceptors/# ตัวแอบแปะ Token ไปกับทุก API
 │    └── services/    # Service กลาง เช่น AuthService
 │
 ├── shared/           # ของที่ใช้ซ้ำหลายๆ หน้า: 
 │    ├── components/  # ปุ่มกดสวยๆ, ตาราง, แจ้งเตือน (Toast)
 │    └── models/      # 📌 ที่อยู่ของ DTO (ไฟล์ .ts ที่เราเสกมาจาก C# อัตโนมัติ)
 │
 ├── features/         # แยกระบบย่อย (Modules) ให้ชัดเจน:
 │    ├── auth/        # ระบบ Login (login.component.ts)
 │    ├── products/    # ระบบสินค้า (product-list, product-form)
 │    ├── sales/       # ระบบขาย (quotation, sales-order)
 │    └── purchasing/  # ระบบจัดซื้อ (supplier, purchase-order)
 │
 ├── layout/           # โครงหน้าจอหลัก: (sidebar.component, header.component)
 └── app.routes.ts     # แผนที่ของแอป (จัดการการเปลี่ยนหน้าลิงก์ไปหน้าต่างๆ)
```

---

### 💡 เกร็ดความรู้เรื่อง DTO:
*   เมื่อคุณสร้าง `ProductCreateDto.cs` ในโฟลเดอร์ **backend/DTOs**
*   เราจะใช้เครื่องมือแปลงมันกลายเป็นไฟล์ `product-create.dto.ts` แล้วเอามาวางไว้ใน **frontend/src/app/shared/models** อัตโนมัติครับ
*   เวลาที่คุณเขียนโค้ดหน้า Angular (ในโฟลเดอร์ features) คุณก็แค่ดึงโมเดลจาก shared/models มาใช้ได้เลยครับ!
