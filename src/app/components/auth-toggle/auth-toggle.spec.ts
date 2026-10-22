import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthToggle } from './auth-toggle';

describe('AuthToggle', () => {
  let component: AuthToggle;
  let fixture: ComponentFixture<AuthToggle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthToggle],
    }).compileComponents();

    fixture = TestBed.createComponent(AuthToggle);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
