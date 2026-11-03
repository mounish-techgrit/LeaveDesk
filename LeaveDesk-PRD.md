# LeaveDesk — Leave Management System PRD (.NET Bootcamp Capstone)

## Overview

LeaveDesk is a leave management system for a company. Employees apply for leave, managers approve or reject it, and HR manages employees, leave policies and holidays. Every key event triggers an email. Build it as a REST API and present it in a demo.

## Goals

- Replace email and spreadsheet based leave tracking with one system.
- Give employees instant visibility of their leave balance and request status.
- Give managers a clear view of who is away and what needs their approval.
- Give HR control over policies, holidays and leave reports.

## Users

| User type | Description |
| --- | --- |
| Employee | Applies for leave and tracks own balance and history |
| Manager | An employee who also approves or rejects their team's leave |
| HR Admin | Manages employees, departments, leave types, holidays and reports |

## Requirements

**Account access**

1. All users log in with email and password, and can log out.
2. Users can reset a forgotten password through email.
3. Accounts are temporarily locked after repeated failed login attempts.

**HR Admin**

4. Create, view, update and deactivate employees; assign their department, manager and role.
5. Create, view, update and delete departments.
6. Create, view, update and delete leave types (e.g. Casual, Sick, Earned, Unpaid) with a yearly quota.
7. Create, view, update and delete public holidays for the year.
8. View all leave requests across the company, with search, filters and paging.
9. Manually adjust an employee's leave balance with a reason.
10. View leave reports by department and month, and export them as CSV.

**Employee**

11. View own profile and update contact details.
12. View leave balance for each leave type.
13. Apply for leave: leave type, from and to date, full or half day, reason.
14. Edit or withdraw a request while it is still pending.
15. Cancel approved leave that has not started yet.
16. View own leave history and status.
17. View the company holiday list.

**Manager**

18. View pending requests from their direct team.
19. Approve or reject a request with a comment.
20. View a team leave calendar for any date range.
21. Apply for their own leave like any employee.

**Notifications (email)**

22. Welcome email when HR creates an employee, with a link to set their password.
23. Email to the manager when a team member submits a request.
24. Email to the employee when their request is approved, rejected or cancelled.
25. Daily reminder to managers for requests pending more than 2 working days.

## Business rules

1. Leave days exclude weekends and public holidays.
2. A request cannot exceed the available balance, except Unpaid leave.
3. A request cannot overlap an existing pending or approved request.
4. Leave cannot be applied for past dates, except Sick leave up to 3 days back.
5. A manager cannot approve their own leave; it goes to their own manager, or HR if they have none.
6. A manager can act only on requests from their direct team.
7. Withdrawing, rejecting or cancelling a request restores the balance.
8. Balances reset every year; up to 10 unused Earned leave days carry forward.
9. Employees see only their own data; managers see only their team; HR sees everything.
10. Deactivated employees cannot log in, and their pending requests are auto-cancelled.
11. Each email address belongs to only one employee.
12. Every approval, rejection and balance adjustment is recorded with who did it and when.

## Must use

- .NET 10 with ASP.NET Core Web API
- OOP and SOLID principles
- Entity Framework Core with a relational database
- ASP.NET Core Identity
- Authentication (JWT)
- Role-based and ownership-based authorization
- Dependency Injection with correct service lifetimes
- Interfaces so services (like email) can be swapped
- Background or scheduled job (reminders)
- Swagger
- Logging
- Global exception handling and input validation
- Proper project structure and coding standards
- Unit testing

## Deliverables

- [ ] Source code in Git
- [ ] README with setup steps
- [ ] Seed data: 1 HR Admin, 2 managers, 6 employees, leave types and holidays
- [ ] Working Swagger UI
- [ ] Demo with Q&A
