import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, expand, finalize, reduce, takeUntil } from 'rxjs';
import { API_URL, AuthService } from '../../../core/auth.service';
interface Page<T> { items:T[]; totalCount:number; pageNumber:number; totalPages:number; }
interface Employee { id:number; fullName:string; isActive:boolean; }
interface Product { id:number; name:string; }
interface Ticket { id:number; productName:string; clientName:string; problemDescription:string; status:string; assignedEmployeeId:number|null; assignedEmployeeName:string|null; createdAt:string; isConfirmedByClient:boolean; isResolvedByEmployee:boolean; }
@Component({selector:'app-manager-tickets',imports:[DatePipe,ReactiveFormsModule,RouterLink],templateUrl:'./manager-tickets.html',styleUrl:'./manager-tickets.css'})
export class ManagerTickets {
  readonly auth=inject(AuthService);
  private readonly http=inject(HttpClient);
  private readonly router=inject(Router);
  private readonly destroyRef=inject(DestroyRef);
  private readonly fb=inject(FormBuilder);
  private readonly cancelList=new Subject<void>();
  readonly result=signal<Page<Ticket>|null>(null);
  readonly employees=signal<Employee[]>([]);
  readonly products=signal<Product[]>([]);
  readonly clients=signal<{id:number;fullName:string}[]>([]);
  readonly clientsError=signal('');
  readonly clientsLoading=signal(false);
  readonly loading=signal(false);
  readonly peopleLoading=signal(false);
  readonly peopleError=signal('');
  readonly productsError=signal('');
  readonly error=signal('');
  readonly success=signal('');
  readonly selected=signal<Ticket|null>(null);
  readonly saving=signal(false);
  readonly assignmentError=signal('');
  readonly filters=this.fb.nonNullable.group({search:[''],status:[''],productId:[''],employeeId:[''],clientId:['']});
  readonly assignee=this.fb.nonNullable.control(0);
  private applied:Record<string,string>={};
  private page=1;
  constructor(){this.loadClients();this.loadEmployees();this.loadProducts();this.load();}
  loadClients():void{
    if(this.clientsLoading())return;
    this.clientsLoading.set(true);this.clientsError.set('');
    const fetch=(page:number)=>this.http.get<Page<{id:number;fullName:string}>>(API_URL+'/clients',{params:{PageNumber:page,PageSize:100}});
    fetch(1).pipe(expand(page=>page.pageNumber<page.totalPages?fetch(page.pageNumber+1):EMPTY),reduce((all:{id:number;fullName:string}[],page)=>all.concat(page.items),[]),takeUntilDestroyed(this.destroyRef),finalize(()=>this.clientsLoading.set(false))).subscribe({next:data=>this.clients.set(data),error:()=>this.clientsError.set('Could not load clients.')});
  }
  loadProducts():void{
    this.productsError.set('');
    this.http.get<Product[]>(API_URL+'/products').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:data=>this.products.set(data),error:()=>this.productsError.set('Could not load systems.')});
  }
  loadEmployees():void{
    if(this.peopleLoading())return;
    this.peopleLoading.set(true);this.peopleError.set('');
    const fetch=(page:number)=>this.http.get<Page<Employee>>(API_URL+'/employees',{params:{PageNumber:page,PageSize:100}});
    fetch(1).pipe(expand(page=>page.pageNumber<page.totalPages?fetch(page.pageNumber+1):EMPTY),reduce((all:Employee[],page)=>all.concat(page.items),[]),takeUntilDestroyed(this.destroyRef),finalize(()=>this.peopleLoading.set(false))).subscribe({next:data=>this.employees.set(data),error:()=>this.peopleError.set('Could not load employees. Retry before assigning a ticket.')});
  }
  applyFilters():void{
    if(this.saving())return;
    const value=this.filters.getRawValue();
    this.applied={};
    if(value.search.trim())this.applied['Search']=value.search.trim().slice(0,200);
    if(value.status)this.applied['Status']=value.status;
    if(value.productId)this.applied['ProductId']=value.productId;
    if(value.employeeId)this.applied['EmployeeId']=value.employeeId;
    if(value.clientId)this.applied['ClientId']=value.clientId;
    this.selected.set(null);this.success.set('');this.load(1);
  }
  resetFilters():void{if(this.saving())return;this.filters.reset();this.applyFilters();}
  load(page=this.page):void{
    if(this.saving())return;
    this.cancelList.next();this.page=page;this.loading.set(true);this.error.set('');this.result.set(null);
    this.http.get<Page<Ticket>>(API_URL+'/tickets',{params:{...this.applied,PageNumber:page,PageSize:10}}).pipe(takeUntil(this.cancelList),takeUntilDestroyed(this.destroyRef),finalize(()=>this.loading.set(false))).subscribe({
      next:data=>{if(!data.items.length&&page>1){this.load(page-1);return;}this.result.set(data);},
      error:()=>this.error.set('Could not load tickets. Please try again.')
    });
  }
  openAssignment(ticket:Ticket):void{
    if(this.saving()||ticket.status==='Closed')return;
    this.selected.set(ticket);this.assignee.setValue(ticket.assignedEmployeeId??0);this.assignmentError.set('');this.success.set('');
  }
  closeAssignment():void{if(!this.saving())this.selected.set(null);}
  assign():void{
    const ticket=this.selected();const employee=this.employees().find(e=>e.id===Number(this.assignee.value)&&e.isActive);
    if(this.saving()||!ticket||ticket.status==='Closed')return;
    if(!employee||this.peopleLoading()||this.peopleError()){this.assignmentError.set('Choose an active support employee.');return;}
    if(employee.id===ticket.assignedEmployeeId){this.assignmentError.set('This ticket is already assigned to this employee.');return;}
    this.saving.set(true);this.assignmentError.set('');
    this.http.patch<Ticket>(API_URL+'/tickets/'+ticket.id+'/assignment',{employeeId:employee.id}).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.saving.set(false))).subscribe({
      next:()=>{this.saving.set(false);this.selected.set(null);this.success.set('Ticket #'+ticket.id+' assigned to '+employee.fullName+'.');this.load();},
      error:(response:HttpErrorResponse)=>{
        this.assignmentError.set(response.status===409?'This ticket changed or was closed. Close this panel and refresh the list.':response.status===400?'This employee may no longer be active. Close this panel and refresh employees.':'Assignment failed. Please try again.');
      }
    });
  }
  logout():void{if(this.saving())return;this.auth.logout();void this.router.navigateByUrl('/login');}
}


