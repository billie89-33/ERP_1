# Jamine ERP: Basic Flow Implementation Plan

เอกสารสรุปแผนงานการพัฒนาระบบ ERP ในส่วนของ "Basic Flow (ระบบสั่งซื้อและคลังสินค้าพื้นฐาน)" เพื่อใช้เป็นแนวทางในการพัฒนาต่อให้จบ Loop สมบูรณ์

---

## 1. โครงสร้างฐานข้อมูล (Entity & Attributes)

### 1.1 Products (สินค้า)
* `Id` (PK)
* `SKU`, `Name`, `Price`, `Cost`
* `OnHandQuantity` (สต๊อกจริงที่มีในโกดัง)
* `ReservedQuantity` (สต๊อกที่ถูกจองจากบิลขาย Pending)
* *(Available Quantity จะถูกคำนวณจาก OnHand - Reserved)*

### 1.2 SalesOrders (ใบสั่งขาย)
* `Id` (PK)
* `OrderNumber` (เช่น SO20260921-001)
* `CustomerId` (FK -> Customers)
* `TotalAmount` (ยอดรวม)
* `Status` (Pending, Shipped, Cancelled)
* `CreatedByUserId` (FK -> Users)

### 1.3 SalesOrderItems (รายการสินค้าในใบสั่งขาย)
* `Id` (PK)
* `SalesOrderId` (FK -> SalesOrders)
* `ProductId` (FK -> Products)
* `Quantity` (จำนวน)
* `UnitPrice` (ราคาต่อหน่วย)

### 1.4 GoodsReceipts (ใบรับสินค้าเข้าคลัง - GR)
* `Id` (PK)
* `GrNumber` (เลขที่ใบรับของ)
* `PurchaseOrderId` (FK -> PurchaseOrders)
* `ReceivedByUserId` (FK -> Users)

### 1.5 GoodsIssues (ใบตัดสินค้าออกจากคลัง - GI)
* `Id` (PK)
* `GiNumber` (เลขที่ใบตัดของ)
* `SalesOrderId` (FK -> SalesOrders)
* `IssuedByUserId` (FK -> Users)

---

## 2. โครงสร้าง API (Backend Endpoints)

**[Sales & Purchasing]**
* `POST /api/SalesOrders` : สร้างบิลขาย **(ทำงานด้วย Transaction: ตรวจสอบ Available Stock -> ตัด Available -> เพิ่ม Reserved)**
* `POST /api/PurchaseOrders` : สร้างบิลซื้อ

**[Warehouse Movement]**
* `POST /api/GoodsReceipts` : รับของเข้าคลัง 
  * *Logic:* รับค่า Quantity ที่กรอกมือ -> นำไปบวกเพิ่มใน `OnHandQuantity` ของ Product -> อัปเดตสถานะ PO เป็น Received
* `POST /api/GoodsIssues` : ส่งของตัดคลัง 
  * *Logic:* รับค่า Quantity ที่กรอกมือ -> นำไปลบออกจาก `OnHandQuantity` **และ** ลบออกจาก `ReservedQuantity` -> อัปเดตสถานะ SO เป็น Shipped

---

## 3. หน้าจอผู้ใช้งาน (Frontend UI)

### 3.1 ฝั่งขาย / จัดซื้อ (Sales / Purchasing)
* **SO / PO Forms:** แบบฟอร์มสำหรับเลือกสินค้า, คำนวณยอดรวมอัตโนมัติ (Subtotal/Total), และกดยืนยันเพื่อสร้างบิล
* **SO / PO Detail Page:** หน้ากดดูรายละเอียดบิลว่าสั่งอะไรไปบ้าง (Read-only)

### 3.2 ฝั่งคลังสินค้า (Warehouse)
* **Goods Receipt (GR) Form:** 
  * แบบฟอร์มสำหรับพนักงานรับของ เลือกบิล PO ที่กำลัง Pending
  * โชว์รายการสินค้าที่สั่งซื้อ และมีช่อง Input ให้ **"กรอกจำนวนที่รับจริง"**
  * กดปุ่ม "Receive Goods" เพื่ออัปเดตสต๊อก `OnHand`
* **Goods Issue (GI) Form:**
  * แบบฟอร์มสำหรับพนักงานแพ็คของ เลือกบิล SO ที่กำลัง Pending
  * โชว์รายการสินค้าที่ต้องส่ง และมีช่อง Input ให้ **"กรอกจำนวนที่ส่งจริง"**
  * กดปุ่ม "Ship Items" เพื่อเคลียร์สต๊อก `OnHand` และใบจอง `Reserved`

---

## 4. สรุป Workflow การทำงานจริง
1. **[เซลส์เปิดบิล]** เซลส์สร้าง SO จำนวน 5 ชิ้น -> `Reserved` +5 
2. **[คลังแพ็คของ]** โกดังเปิดหน้า GI Form เลือกบิล SO นั้น 
3. **[คลังใส่ยอดจริง]** โกดังกรอกยอดจัดส่ง 5 ชิ้น และกด Submit
4. **[ระบบตัดสต๊อก]** ระบบหลังบ้านจะหักยอด `OnHand` -5 และหักยอด `Reserved` -5 
5. **[ปิดจ๊อบ]** บิล SO เปลี่ยนสถานะเป็น Shipped 

*(จบกระบวนการ Basic ERP Flow)*
