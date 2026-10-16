import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL, AuthService } from '../../../core/auth.service';
interface DashboardData {
 totalTickets:number; totalClients:number; activeClients:number; totalEmployees:number; activeEmployees:number;
 statusCounts:{status:string;count:number}[];
 employeeProductivity:{employeeId:number;fullName:string;isActive:boolean;assignedTickets:number;openTickets:number;closedTickets:number}[];
 ticketsByMonth:{year:number;month:number;createdTickets:number}[];
 productivityDefinition:string; generatedAt:string;
}
@Component({selector:'app-manager-dashboard',imports:[DatePipe,RouterLink],templateUrl:'./dashboard.html',styleUrl:'./dashboard.css'})
export class ManagerDashboard {
 readonly auth=inject(AuthService);
 private readonly http=inject(HttpClient);
 private readonly router=inject(Router);
 private readonly destroyRef=inject(DestroyRef);
 readonly data=signal<DashboardData|null>(null);
 readonly loading=signal(false);
 readonly error=signal('');
 readonly monthlyMax=computed(()=>Math.max(1,...(this.data()?.ticketsByMonth.map(m=>m.createdTickets)??[])));
 readonly closed=computed(()=>this.data()?.statusCounts.find(s=>s.status==='Closed')?.count??0);
 readonly open=computed(()=>Math.max(0,(this.data()?.totalTickets??0)-this.closed()));
 constructor(){this.load();}
 load():void{
  if(this.loading())return;
  this.loading.set(true);this.error.set('');
  this.http.get<DashboardData>(API_URL+'/dashboard').pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.loading.set(false))).subscribe({next:data=>this.data.set(data),error:()=>this.error.set('Could not refresh the dashboard. Please try again.')});
 }
 percentage(count:number):number{const total=this.data()?.totalTickets??0;return total>0?Math.round(count/total*100):0;}
 monthLabel(year:number,month:number):string{return new Date(year,month-1,1).toLocaleDateString('en',{month:'short',year:'2-digit'});}
 logout():void{this.auth.logout();void this.router.navigateByUrl('/login');}
}
