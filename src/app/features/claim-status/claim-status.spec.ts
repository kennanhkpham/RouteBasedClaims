import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClaimStatus } from './claim-status';

describe('ClaimStatus', () => {
  let component: ClaimStatus;
  let fixture: ComponentFixture<ClaimStatus>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimStatus],
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimStatus);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
