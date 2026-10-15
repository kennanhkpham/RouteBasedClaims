import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ClaimIntakeComponent } from './claim-intake';

describe('ClaimIntakeComponent', () => {
  let component: ClaimIntakeComponent;
  let fixture: ComponentFixture<ClaimIntakeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimIntakeComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimIntakeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
