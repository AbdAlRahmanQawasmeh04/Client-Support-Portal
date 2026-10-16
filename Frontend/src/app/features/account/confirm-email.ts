import { Component, DestroyRef, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL, AuthService } from '../../core/auth.service';

@Component({selector:'app-confirm-email',imports:[RouterLink],styleUrl:'./account.css',template:`
  <main class="confirmation card" lang="en" dir="ltr"><p class="eyebrow">ACCOUNT SECURITY</p><h1>Confirm email change</h1>
  @if(done()){<p class="success" role="status">Your email has been changed. Sign in again using your new email or username and your existing password.</p><a routerLink="/login">Back to sign in</a>}
  @else if(!hasToken){<p class="error">The confirmation link is missing. Open the link in your email or request a new one from My account.</p>}
  @else{<p>Select the button below to confirm your new email address. This will sign out existing sessions.</p><button class="primary" [disabled]="busy()" (click)="confirm()">{{busy()?'Confirming…':'Confirm email change'}}</button>}
  @if(error()){<p class="error" role="alert">{{error()}}</p>}
  <p><a routerLink="/my-tickets/account">My account</a></p></main>`})
export class ConfirmEmail {
  private readonly http=inject(HttpClient);private readonly auth=inject(AuthService);private readonly destroyRef=inject(DestroyRef);
  private token=new URLSearchParams(inject(ActivatedRoute).snapshot.fragment??'').get('token')??'';
  readonly hasToken=this.token.length>0&&this.token.length<=8192;
  readonly busy=signal(false);readonly done=signal(false);readonly error=signal('');
  constructor(){inject(Location).replaceState('/confirm-email');this.destroyRef.onDestroy(()=>this.token='');}
  confirm():void{
    if(this.busy()||this.done()||!this.hasToken)return;
    this.busy.set(true);this.error.set('');
    this.http.post(API_URL+'/account/confirm-email',{token:this.token}).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.busy.set(false))).subscribe({
      next:()=>{this.token='';this.auth.logout();this.done.set(true);},
      error:(response:HttpErrorResponse)=>this.error.set(response.status===0?'Cannot reach the server. Try again.':response.status===429?'Too many attempts. Try again in 15 minutes.':'This link is invalid, expired, already used, or the email is unavailable. Request a new link from My account.')
    });
  }
}
