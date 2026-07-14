import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import Swal from 'sweetalert2';
import { jwtDecode } from 'jwt-decode';
import { Route, Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  loginForm!:FormGroup
  errorMessage: any;
  constructor(
    private readonly auth:AuthService,
    private readonly fb:FormBuilder,
    private readonly router:Router
  ){}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      username:['',Validators.required],
      password:['',Validators.required]
    });
  }

  onLogin(){

    if (this.loginForm.invalid) {
      Swal.fire({
        icon: 'warning',
        title: 'Invalid Form',
        text: 'Please enter username and password'
      });
      return;
    }
    const loginData = this.loginForm.value;
    this.auth.login(loginData).subscribe({
      next: (res: any) => {

        // Success alert
        Swal.fire({
          icon: 'success',
          title: 'Login Successful',
          timer: 1200,
          showConfirmButton: false,
          didClose:()=>{
            //  const decodeToken: any = jwtDecode(res.data.token);
            // const userRole =
            //   decodeToken.role ||
            //   decodeToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

            // if (!userRole) {
            //   this.errorMessage.set('Role not found in token');
            //   return;
            // }
            // this.auth.setRole(userRole);
            // if(userRole == 'admin' || userRole == 'manager')
            //   this.router.navigate(['/home/admin']);
            // else if(userRole == 'employee' || userRole == 'hr' || userRole == 'Team Lead' || userRole == 'Sales' || userRole == 'Operations')
            //   this.router.navigate(['/home/employee'])
            // else
            //   this.router.navigate(['/error'])
            console.log(res);

            if (!userRole) {
              this.errorMessage.set('Role not found in token');
              return;
            }
            this.auth.setRole(userRole);
            
            if(userRole == 'admin' || userRole == 'manager')
              this.router.navigate(['/home/admin']);
            else if(userRole == 'employee'||userRole=='hr')
              this.router.navigate(['/home/employee'])
            else
              this.router.navigate(['/error'])
          }
        });

      },

      error: (err) => {
        Swal.fire({
          icon: 'error',
          title: 'Login Failed',
          text: err?.error?.message || 'Invalid username or password'
        });
      }
    });
  }

}
