import { validBirthDate } from '../../../core/birth-date-validator';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL, AuthService } from '../../../core/auth.service';
interface Client {id:number;fullName:string;username:string;email:string;mobileNumber:string;isActive:boolean;ticketCount:number;}
interface Details extends Client {dateOfBirth:string;address:string;imagePath:string|null;}
interface ClientPage {items:Client[];totalCount:number;pageNumber:number;totalPages:number;}
@Component({selector:'app-manager-clients',imports:[ReactiveFormsModule,RouterLink],templateUrl:'./clients.html',styleUrls:['../dashboard/dashboard.css','./clients.css']})
export class ManagerClients {
 readonly auth=inject(AuthService);private readonly http=inject(HttpClient);private readonly router=inject(Router);private readonly fb=inject(FormBuilder);private readonly destroyRef=inject(DestroyRef);
 readonly result=signal<ClientPage|null>(null);readonly loading=signal(false);readonly detailLoading=signal(false);readonly busy=signal(false);readonly error=signal('');readonly detailError=signal('');readonly success=signal('');readonly selected=signal<Details|null>(null);readonly photo=signal('');readonly photoFailed=signal(false);readonly pendingStatus=signal(false);
 readonly search=this.fb.nonNullable.control('');private appliedSearch='';private page=1;private selectedId=0;private imageVersion=0;
 readonly today=(()=>{const date=new Date();return `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;})();
 readonly form=this.fb.nonNullable.group({fullName:['',Validators.required],username:['',Validators.required],email:['',[Validators.required,Validators.email]],mobileNumber:['',[Validators.required,Validators.pattern(/^\+?[0-9() .-]{6,25}$/)]],dateOfBirth:['', [Validators.required, validBirthDate]],address:['',Validators.required]});
 constructor(){this.load();this.destroyRef.onDestroy(()=>this.clearPhoto());}
 private clearPhoto():void{if(this.photo())URL.revokeObjectURL(this.photo());this.photo.set('');this.photoFailed.set(false);}
 load(page=this.page):void{if(this.loading()||this.busy())return;this.page=page;this.loading.set(true);this.error.set('');this.result.set(null);this.http.get<ClientPage>(API_URL+'/clients',{params:{PageNumber:page,PageSize:10,Search:this.appliedSearch}}).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.loading.set(false))).subscribe({next:data=>this.result.set(data),error:()=>this.error.set('Could not load clients. Please try again.')});}
 applySearch():void{if(this.loading()||this.busy())return;this.appliedSearch=this.search.value.trim().slice(0,200);this.load(1);}
 open(id:number):void{
  if(this.busy()||this.detailLoading())return;
  if(this.form.dirty&&!window.confirm('Discard the unsaved client changes?'))return;
  this.selectedId=id;this.imageVersion++;this.selected.set(null);this.clearPhoto();this.detailError.set('');this.success.set('');this.pendingStatus.set(false);this.form.reset();this.detailLoading.set(true);
  this.http.get<Details>(API_URL+'/clients/'+id).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.detailLoading.set(false))).subscribe({next:data=>{this.selected.set(data);this.form.reset({fullName:data.fullName,username:data.username,email:data.email,mobileNumber:data.mobileNumber,dateOfBirth:data.dateOfBirth.slice(0,10),address:data.address});this.loadPhoto(id);},error:()=>this.detailError.set('Could not load this client. Please retry.')});
 }
 retryDetails():void{if(this.selectedId)this.open(this.selectedId);}
 close():void{if(this.busy()||this.detailLoading())return;if(this.form.dirty&&!window.confirm('Discard the unsaved client changes?'))return;this.selectedId=0;this.imageVersion++;this.selected.set(null);this.detailError.set('');this.pendingStatus.set(false);this.form.reset();this.clearPhoto();}
 private loadPhoto(id:number):void{
  const version=++this.imageVersion;
  this.http.get(API_URL+'/users/'+id+'/image',{responseType:'blob'}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:blob=>{if(version!==this.imageVersion||id!==this.selectedId)return;if(!['image/png','image/jpeg'].includes(blob.type))return;this.clearPhoto();this.photo.set(URL.createObjectURL(blob));},error:()=>{}});
 }
 private fail(response:HttpErrorResponse):void{this.detailError.set(response.status===409?'This username or email is already used.':response.status===400&&typeof response.error==='string'?response.error:response.status===404?'This client no longer exists.':'Could not save the change. Please try again.');}
 save():void{
  const client=this.selected();if(!client||this.busy()||!this.form.dirty)return;this.detailError.set('');this.success.set('');this.form.markAllAsTouched();const value=this.form.getRawValue();
  if(this.form.invalid||Object.values(value).some(v=>!v.trim())||value.dateOfBirth>this.today){this.detailError.set('Complete all fields, use a valid email and phone number, and choose a birth date that is not in the future.');return;}
  const body=new FormData();for(const [key,text] of Object.entries(value))body.append(key[0].toUpperCase()+key.slice(1),text.trim());
  this.busy.set(true);this.http.put(API_URL+'/clients/'+client.id,body).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.busy.set(false))).subscribe({next:()=>{this.selected.set({...client,...value});this.form.markAsPristine();this.success.set('Client details saved.');this.busy.set(false);this.load();},error:response=>this.fail(response)});
 }
 toggleStatus():void{
  const client=this.selected();if(!client||this.busy()||!this.pendingStatus())return;
  const body=new FormData();body.append('isActive',String(!client.isActive));this.busy.set(true);this.detailError.set('');this.success.set('');
  this.http.patch(API_URL+'/clients/'+client.id+'/active-status',body).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.busy.set(false))).subscribe({next:()=>{this.selected.set({...client,isActive:!client.isActive});this.pendingStatus.set(false);this.success.set(client.isActive?'Client account deactivated.':'Client account activated.');this.busy.set(false);this.load();},error:response=>this.fail(response)});
 }
 upload(event:Event):void{
  const client=this.selected();const input=event.target as HTMLInputElement;const file=input.files?.[0];if(!client||this.busy()||!file)return;this.detailError.set('');this.success.set('');
  if(!/\.(png|jpe?g)$/i.test(file.name)||!['image/png','image/jpeg'].includes(file.type)||file.size===0||file.size>2*1024*1024){this.detailError.set('Choose a JPG or PNG image, up to 2 MB.');input.value='';return;}
  const body=new FormData();body.append('image',file);this.busy.set(true);
  this.http.put(API_URL+'/clients/'+client.id+'/image',body).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>{this.busy.set(false);input.value='';})).subscribe({next:()=>{this.success.set('Client photo updated.');this.loadPhoto(client.id);},error:response=>this.fail(response)});
 }
 logout():void{if(this.busy())return;if(this.form.dirty&&!window.confirm('Discard the unsaved client changes and sign out?'))return;this.auth.logout();void this.router.navigateByUrl('/login');}
}

