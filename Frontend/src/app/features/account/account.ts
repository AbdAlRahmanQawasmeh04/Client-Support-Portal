import { Component, DestroyRef, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';
import { API_URL, AuthService } from '../../core/auth.service';

interface Account { id:number; fullName:string; username:string; email:string; mobileNumber:string; address:string; dateOfBirth:string; hasImage:boolean; }
@Component({selector:'app-account', imports:[ReactiveFormsModule,RouterLink],templateUrl:'./account.html',styleUrl:'./account.css'})
export class AccountPage {
  readonly auth=inject(AuthService);
  private readonly http=inject(HttpClient);
  private readonly fb=inject(FormBuilder);
  private readonly destroyRef=inject(DestroyRef);
  readonly account=signal<Account|null>(null);
  readonly loading=signal(false);
  readonly busy=signal(false);
  readonly error=signal('');
  readonly success=signal('');
  readonly signedOut=signal(false);
  readonly photo=signal('');
  readonly photoError=signal('');
  private photoVersion=0;
  readonly profile=this.fb.nonNullable.group({fullName:['',[Validators.required,Validators.maxLength(150)]],mobileNumber:['',[Validators.required,Validators.pattern(/^\+?[0-9() .-]{6,25}$/)]],address:['',[Validators.required,Validators.maxLength(500)]]});
  readonly password=this.fb.nonNullable.group({currentPassword:['',[Validators.required,Validators.maxLength(1024)]],newPassword:['',[Validators.required,Validators.minLength(8),Validators.maxLength(128)]],confirmPassword:['',Validators.required]});
  readonly email=this.fb.nonNullable.group({newEmail:['',[Validators.required,Validators.email,Validators.maxLength(254)]],currentPassword:['',[Validators.required,Validators.maxLength(1024)]]});
  constructor(){this.load();this.destroyRef.onDestroy(()=>this.clearPhoto());}
  private clearPhoto(){const old=this.photo();if(old)URL.revokeObjectURL(old);this.photo.set('');}
  load():void {
    if(this.loading()||this.busy())return;
    this.loading.set(true);this.error.set('');
    this.http.get<Account>(API_URL+'/account').pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.loading.set(false)))
      .subscribe({next:data=>{this.account.set(data);this.profile.reset({fullName:data.fullName,mobileNumber:data.mobileNumber,address:data.address});this.loadPhoto();},error:()=>this.error.set('Could not load your account. Please retry.')});
  }
  loadPhoto():void {
    const data=this.account();if(!data)return;
    const version=++this.photoVersion;this.clearPhoto();this.photoError.set('');
    if(!data.hasImage)return;
    this.http.get(API_URL+'/users/'+data.id+'/image',{responseType:'blob'}).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next:blob=>{if(version===this.photoVersion && ['image/png','image/jpeg'].includes(blob.type))this.photo.set(URL.createObjectURL(blob));},
      error:()=>{if(version===this.photoVersion)this.photoError.set('Could not load your photo.');}
    });
  }
  private send(request:Observable<unknown>, complete:()=>void):void {
    this.busy.set(true);this.error.set('');this.success.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.busy.set(false))).subscribe({next:complete,error:(response:HttpErrorResponse)=>{
      this.error.set(typeof response.error?.message==='string'?response.error.message:response.status===0?'Cannot reach the server. Try again.':response.status===429?'Too many attempts. Try again in 15 minutes.':'Could not save changes. Check the fields and try again.');
    }});
  }
  saveProfile():void {
    if(this.busy()||!this.account())return;
    this.profile.markAllAsTouched();const value=this.profile.getRawValue();
    if(this.profile.invalid||Object.values(value).some(v=>!v.trim())){this.error.set('Enter your name, phone number (digits 0-9) and address.');return;}
    const body={fullName:value.fullName.trim(),mobileNumber:value.mobileNumber.trim(),address:value.address.trim()};
    this.send(this.http.put(API_URL+'/account',body),()=>{
      this.account.update(data=>data?{...data,...body}:data);this.profile.reset(body);this.auth.updateProfile(body.fullName);this.success.set('Account details saved.');
    });
  }
  upload(event:Event):void {
    const input=event.target as HTMLInputElement;const file=input.files?.[0];input.value='';
    if(!file||this.busy())return;
    if(!['image/png','image/jpeg'].includes(file.type)||file.size===0||file.size>2*1024*1024){this.error.set('Choose a JPG or PNG image, up to 2 MB.');return;}
    const body=new FormData();body.append('Image',file,file.name);
    this.send(this.http.put(API_URL+'/account/photo',body),()=>this.photoSaved(true));
  }
  removePhoto():void{if(this.busy())return;this.send(this.http.delete(API_URL+'/account/photo'),()=>this.photoSaved(false));}
  private photoSaved(hasImage:boolean):void{
    this.account.update(data=>data?{...data,hasImage}:data);this.loadPhoto();this.auth.profileChanged.next();this.success.set(hasImage?'Photo updated.':'Photo removed.');
  }
  changePassword():void {
    if(this.busy())return;this.password.markAllAsTouched();const value=this.password.getRawValue();
    if(this.password.invalid||value.newPassword!==value.confirmPassword||value.newPassword===value.currentPassword){this.error.set('Enter your current password and a different new password of 8 to 128 characters. Both new passwords must match.');return;}
    this.send(this.http.post(API_URL+'/account/password',value),()=>{this.password.reset();this.email.reset();this.auth.logout();this.signedOut.set(true);this.success.set('Password changed. Sign in again with your new password.');});
  }
  changeEmail():void{
    if(this.busy())return;this.email.markAllAsTouched();const value=this.email.getRawValue();
    if(this.email.invalid||value.newEmail.trim().toLowerCase()===this.account()?.email.toLowerCase()){this.error.set('Enter a different valid email address and your current password.');return;}
    this.send(this.http.post(API_URL+'/account/email',{newEmail:value.newEmail.trim(),currentPassword:value.currentPassword}),()=>{this.email.reset();this.success.set('Check your new email for a confirmation link. Your current email stays active until you confirm.');});
  }
}
