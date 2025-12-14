# 🔧 Mechanical Workshop Management System

## 📖 Project Overview

The **Mechanical Workshop Management System** is designed to manage and organize workshop operations in a clean, scalable, and maintainable way. The project focuses heavily on **Business Logic enforcement**, clear separation of concerns, and modern backend architecture patterns.

The system helps manage:

* **Customers and their Vehicles**
* **Repair Tasks** associated with each vehicle
* **Work Orders** lifecycle and status transitions
* **Labors / Employees** assignments
* **Authorization & Access Control** based on roles and policies

The goal of the project is not just CRUD operations, but a **real-world, production-ready architecture** that reflects best practices.

---

## 🏗️ Architecture

The project is built using:

### ✅ Clean Architecture

* Clear separation between **Domain**, **Application**, **Infrastructure**, and **API** layers
* Business rules are isolated from external frameworks
* High testability and maintainability

### ✅ CQRS Pattern (Command Query Responsibility Segregation)

* **Commands** for write operations
* **Queries** for read operations
* Improved scalability and clarity of intent

### ✅ Vertical Slice Architecture

* Features are grouped by **use-case**, not by technical concern
* Each slice contains its own:

  * Command / Query
  * Handler
  * Validator
  * Request / Response (Contracts)

---

## 🧠 Core Design Concepts

### 🔹 Result Pattern

* Custom `Result<T>` implementation
* Unified success & failure handling
* Clear error propagation without throwing exceptions in business logic

### 🔹 Business Logic First

* All rules are enforced inside the **Application layer**
* No business logic leaks into Controllers or Infrastructure

---

## 🔐 Security & Authorization

### ✅ Custom Authorization Policies

* Custom authorization policy to **allow only assigned labors** to change a **Work Order status**
* Prevents unauthorized status transitions
* Enforces real business constraints

---

## 🧩 CQRS Pipeline Behaviors

Implemented using **MediatR Pipeline Behaviors**:

* 🔍 **Validation Behavior**

  * Uses FluentValidation
  * Automatically validates commands and queries

* 📊 **Performance Monitoring Behavior**

  * Logs execution time for each request
  * Helps detect slow operations

* 📝 **Logging Behavior**

  * Logs request details for observability and debugging

---

## Global Exception Handling

### ✅ Custom Exception Handling Middleware

* Centralized exception handling
* Consistent error responses
* Clean separation between exception handling and business logic

---

## 🗄️ Data Access Layer

### ✅ Entity Framework Core

* **Code First** approach
* Clear entity configuration
* Migrations used for database evolution

---

## ⚙️ Background Services & Real-Time Features

### 🔄 Background Services

* Background services are used to **periodically check customer appointments**
* If a customer is **delayed beyond the allowed time**, the related **Work Order is automatically canceled**
* Ensures workshop schedule consistency and reduces manual intervention

### 🔔 Real-Time Notifications

* Real-time communication is implemented to **notify the team immediately when a Work Order status changes**
* Enables instant UI updates without manual refresh
* Improves operational awareness and response time

---

## 📦 Project Structure

```text
src/
│
├── MechanicShop.API
│   ├── Controllers
│   ├── Middlewares          
│              
│
├── MechanicShop.Application
│   ├── Common               # Result Pattern, helpers, base abstractions
│   ├── Features             # CQRS Vertical Slices (Commands / Queries / Handlers)
│   └── DependencyInjection.cs
│
├── MechanicShop.Contracts
│   ├── Common              
│   ├── Requests             
│   ├── Responses            
│   └── Class1.cs           
│
├── MechanicShop.Domain
│   ├── Common               
│   ├── Customers           
│   ├── Employees            
│   ├── Identity            
│   ├── RepairTasks          
│   └── WorkOrders           
│
├── MechanicShop.Infrastructure
│   ├── BackgroundJobs       
│   ├── Data            
│   ├── Identity             
│   ├── Migrations           
│   ├── RealTime            
│   ├── Services             
│   ├── Settings             
│   └── DependencyInjection.cs
│
└── tests                    # Unit & integration tests
```

---

## 🎯 Key Highlights

* ✔ Clean Architecture
* ✔ CQRS with MediatR
* ✔ Result Pattern
* ✔ Vertical Slice Architecture
* ✔ Custom Authorization Policies
* ✔ Pipeline Behaviors (Validation, Logging, Performance)
* ✔ Custom Exception Middleware
* ✔ EF Core Code First

---

## 🧪 Project Status

> ✅ **Development Completed**
>
> 🧪 **Currently Under Testing**

---

## 🚀 Final Notes

This project is built as a **learning-focused yet production-oriented system**, emphasizing:

* Correct architecture decisions
* Strong business logic enforcement
* Clean, readable, and maintainable code

It serves as a solid foundation for extending into a full-scale workshop management platform.
