# Jamine ERP & E-Commerce - Master Plan & Blueprint

เอกสารฉบับนี้คือ **แผนผังแม่บท (Master Plan)** สำหรับการพัฒนาระบบ Jamine ERP และ Storefront อย่างเต็มรูปแบบ ครอบคลุมตั้งแต่ Database, Backend API, ไปจนถึง Frontend UI พร้อมกำหนดมาตรฐานการทำงานเพื่อป้องกันข้อผิดพลาด

---

## 1. ฐานข้อมูล (Database & Entities)
ตรวจสอบและอัปเดต Entity ของระบบ Entity Framework Core (C#) เพื่อรองรับระบบคลังสินค้าและการขายครบวงจร

### 📦 Core Entities (จัดการสินค้า)
- [x] `Category` (หมวดหมู่สินค้า)
- [ ] **`Product` (สินค้า) - *[Need Update]***
  - เพิ่ม `OnHandQuantity` (จำนวนจริงในคลัง)
  - เพิ่ม `ReservedQuantity` (จำนวนที่ถูกจองจากใบสั่งขาย)
  - *Computed:* `AvailableQuantity` = OnHand - Reserved
  - ลบ `StockQuantity` ตัวเก่าทิ้ง เพื่อป้องกันความสับสน

### 🏢 People Entities (บุคคลที่เกี่ยวข้อง)
- [x] `User` (พนักงาน/แอดมินระบบ)
- [x] `Customer` (ลูกค้าที่ซื้อผ่านหน้าเว็บ B2C หรือหลังบ้าน B2B)
- [x] `Supplier` (ผู้จัดจำหน่าย/คู่ค้า)

### 🛒 Sales & CRM Entities (ระบบขาย)
- [x] `Quotation` & `QuotationItem` (ใบเสนอราคา)
- [x] `SalesOrder` & `SalesOrderItem` (ใบสั่งขาย - SO)

### 🚚 Procurement Entities (ระบบจัดซื้อ)
- [x] `PurchaseOrder` & `PurchaseOrderItem` (ใบสั่งซื้อ - PO)

### 🏭 Warehouse Entities (ระบบคลังสินค้า) - *[Missing - Need to Create]*
- [ ] **`GoodsReceipt` & `GoodsReceiptItem` (ใบรับเข้า - GR)**
  - `ReferencePOId` (อ้างอิงจากใบสั่งซื้อ)
  - พอกด Completed -> ส่งคำสั่งไปบวก `Product.OnHandQuantity`
- [ ] **`GoodsIssue` & `GoodsIssueItem` (ใบตัดออก - GI)**
  - `ReferenceSOId` (อ้างอิงจากใบสั่งขาย)
  - พอกด Shipped -> ส่งคำสั่งไปลบ `Product.OnHandQuantity` และ `Product.ReservedQuantity`

---

## 2. โครงสร้างหน้าจอ (Frontend Routes & Links)

### 🛍️ Storefront (หน้าร้านสำหรับลูกค้า) - Angular
ทุกหน้าต้องมีลิงก์เชื่อมต่อกันผ่าน Header / Footer และปุ่ม Call to Action
- `/` - **Home:** หน้าแรก โปรโมทสินค้า (✅ Done)
- `/shop` - **Catalog:** ดูสินค้าและกรองสเปค (✅ Done)
- `/product/:id` - **Product Detail:** ดูรายละเอียด (✅ Done)
- `/cart` - **Cart:** ตะกร้าสินค้า (✅ Done)
- `/checkout` - **Checkout:** หน้าชำระเงินและกรอกที่อยู่ **(*[Next Step]* - ส่งข้อมูลไปสร้าง SO)**
- `/profile/orders` - **Order History:** สำหรับลูกค้าติดตามสถานะการจัดส่ง (GI) **(*[Pending]* )**

### ⚙️ ERP Admin (หลังบ้านสำหรับพนักงาน) - Angular
ระบบนำทาง (Navigation Sidebar) ต้องลิงก์เชื่อมถึงกันทั้งหมด
- `/admin/login` - เข้าสู่ระบบ (✅ Done)
- `/admin/dashboard` - แดชบอร์ดสรุปยอด **(*[Pending]* )**
- **Inventory (ระบบคลังสินค้า)**
  - `/admin/inventory` - **Product Catalog:** ดูสต็อก On-Hand, Reserved, Available **(*[Next Step]* )**
  - `/admin/inventory/goods-receipt` - **GR Form:** รับสินค้าเข้าคลังอ้างอิง PO **(*[Next Step]* )**
  - `/admin/inventory/goods-issue` - **GI Overview:** อนุมัติตัดสต็อกส่งของอ้างอิง SO **(*[Next Step]* )**
- **Sales (ระบบขาย)**
  - `/admin/sales/orders` - **Sales Orders:** จัดการ SO (✅ Partial)
- **Procurement (ระบบจัดซื้อ)**
  - `/admin/procurement/orders` - **Purchase Orders:** จัดการ PO (✅ Partial)

---

## 3. มาตรฐานการพัฒนา (Development Standards)
เพื่อให้โค้ดมีคุณภาพและป้องกันบั๊ก ให้ยึดหลักปฏิบัติดังนี้:

1. **🔴 TypeScript Strict Mode (ข้อห้ามเด็ดขาด):** 
   - **ห้ามใช้ `any` หรือ `unknown` เด็ดขาด** ในโค้ด TypeScript ของ Angular 
   - ทุกครั้งที่รับส่งข้อมูลกับ API ต้องมีการสร้าง Interface (Dto) มารองรับเสมอ (เช่น `ProductDto`, `SalesOrderDto`)
   - ต้องกำหนด Type ให้ตัวแปรและฟังก์ชันอย่างชัดเจน เพื่อให้ Compiler ช่วยจับบั๊กก่อนรันโปรแกรม
2. **C# & Null Safety:** 
   - ฝั่ง C# Backend ต้องเปิด Nullable Enable และจัดการ Warning เรื่อง Nullable ให้หมด
3. **Signal Reactive State:** 
   - Angular ให้ใช้ `Signal` สำหรับจัดการ State แทน RxJS แบบเก่า เพื่อให้ UI อัปเดตทันที
4. **Link & Router Check:** 
   - ทุกครั้งที่สร้างหน้าใหม่ ต้องมั่นใจว่ามีการเชื่อมลิงก์ (RouterLink) จากหน้าหลัก (เช่น Header/Sidebar) เข้าไปถึงเสมอ เพื่อไม่ให้เกิดหน้าจอเด็กกำพร้า (Orphan Pages)
5. **Bug Check at Every Step (Debug Mantra):**
   - หลังแก้ Backend -> ทดสอบ API ด้วย Swagger/Postman
   - หลังทำ Frontend -> รัน `ng build` หรือ `ng serve` ตรวจจับ Compile Error ก่อนส่งงาน
   - ถ้าระบบพัง -> หารูรั่วแบบ End-to-End (Trace the Fail Path) ห้ามเดาสุ่ม
6. **Data Seeding First:**
   - สำหรับระบบ ERP (PO, SO, GR, GI) จะต้องทำ Data Seeder จำลองเอกสารขึ้นมาก่อนเริ่มทำ UI เพื่อให้เห็น Data Flow จริง

---

## 4. โครงสร้างหน้าจอและฟอร์ม (UI Forms & Screens)
รายการฟอร์มบนหน้าเว็บที่ต้องสอดคล้องกับ Entity ในฐานข้อมูลอย่างสมบูรณ์แบบ:

### 🛍️ Storefront Forms
- **Checkout Form (อ้างอิง `SalesOrder`, `Customer`):**
  - ฟิลด์ลูกค้า: `FirstName`, `LastName`, `Email`, `Phone`, `ShippingAddress`
  - ข้อมูลตะกร้า (Cart Items) -> แปลงเป็น `SalesOrderItem`
  - สรุปยอด: `SubTotal`, `ShippingFee`, `TotalAmount`

### ⚙️ ERP Admin Forms
- **Product Form (อ้างอิง `Product`):**
  - ข้อมูลพื้นฐาน: `SKU`, `Name`, `Price`, `Cost`, `CategoryId`
  - สเปคเชิงลึก: `Specifications` (JSON)
  - รูปภาพ: `ImageUrl`, `CloudinaryPublicId`
  - *(หมายเหตุ: ห้ามมีช่องให้แก้ OnHand, Reserved สต็อกโดยตรง ต้องแก้ผ่าน GR/GI เท่านั้น)*
- **Goods Receipt Form - GR (อ้างอิง `GoodsReceipt`, `PurchaseOrder`):**
  - ส่วนหัว: `GR Number`, `ReferencePOId`, `ReceiptDate`, `ReceiverName`, `SupplierName`
  - รายการ (Line Items): `SKU`, `ProductName`, `OrderedQty`, `ReceivedQty` (ช่องกรอก)
- **Goods Issue Form - GI (อ้างอิง `GoodsIssue`, `SalesOrder`):**
  - ส่วนหัว: `GI Number`, `ReferenceSOId`, `DeliveryDate`, `CustomerName`, `ShippingAddress`
  - รายการ (Line Items): `SKU`, `ProductName`, `Location/Bin`, `RequiredQty`, `IssuedQty` (ช่องกรอก)

---

## 5. รายการ API ที่ต้องสร้าง (API Endpoints List)
API เหล่านี้ต้องรับส่งข้อมูลผ่าน `Dto` แบบ Strongly-Typed:

**[Storefront]**
- `POST /api/storefront/checkout` -> รับข้อมูลตะกร้า สร้าง `SalesOrder` (สถานะ Pending)

**[ERP Admin - Inventory]**
- `GET /api/inventory/products` -> ดึงรายการสินค้าพร้อมค่า `OnHand`, `Reserved`, `Available`
- `GET /api/inventory/purchase-orders/pending` -> ดึงรายการ PO ที่รอรับเข้า
- `POST /api/inventory/goods-receipt` -> บันทึกใบรับเข้า (GR) **พร้อมอัปเดต OnHand (+)**
- `GET /api/inventory/sales-orders/pending` -> ดึงรายการ SO ที่รอจัดส่ง
- `POST /api/inventory/goods-issue` -> บันทึกใบตัดออก (GI) **พร้อมอัปเดต OnHand (-) และ Reserved (-)**

**[ERP Admin - Dashboard (Future)]**
- `GET /api/dashboard/stats` -> ดึงยอดขาย, สต็อกคงเหลือ, สถานะเอกสาร

---

## 6. การคาดการณ์ผลกระทบ (Impact Analysis)
**เมื่อเราลบฟิลด์ `StockQuantity` และเปลี่ยนเป็น `OnHandQuantity`, `ReservedQuantity`** ส่วนต่างๆ ของระบบจะได้รับผลกระทบดังนี้ (ต้องไปไล่เช็คแก้ไขให้ครบ):

🔴 **1. Frontend Storefront (`ShopComponent` & `ProductDetailComponent`)**
- โค้ดเดิมที่เช็คว่า `if (product.stockQuantity > 0)` จะพังทั้งหมด 
- **วิธีแก้:** ต้องเปลี่ยนไปเช็คจากฟิลด์ `availableQuantity` แทน (ห้ามขายเกินของที่มี Available)

🔴 **2. Frontend Storefront (`CartComponent`)**
- โค้ดเดิมที่ป้องกันการกดปุ่ม `+` เพิ่มจำนวนเกินสต็อกในตะกร้า 
- **วิธีแก้:** ต้องเช็ค Validate ด้วยค่า `availableQuantity` 

🔴 **3. Backend `ProductsController` (GetProducts, GetProductById)**
- ข้อมูลที่ส่งกลับไปหา Storefront จะไม่มี `StockQuantity` แล้ว
- **วิธีแก้:** ใน ProductDto ต้องอัปเดตให้มี `AvailableQuantity` (คำนวณจาก OnHand - Reserved) แล้วส่งกลับไปให้หน้าเว็บแสดงผล

🔴 **4. Backend `MigrationController` (ถ้ามีสคริปต์เก่า)**
- สคริปต์ที่เคย Seed ข้อมูล `StockQuantity` จะ Error 
- **วิธีแก้:** ต้องอัปเดตสคริปต์ Seed Data ให้จำลองค่า `OnHandQuantity` แทน
