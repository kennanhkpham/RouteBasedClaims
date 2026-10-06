import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClaimIntake } from './claim-intake';

describe('ClaimIntake', () => {
  let component: ClaimIntake;
  let fixture: ComponentFixture<ClaimIntake>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClaimIntake],
    }).compileComponents();

    fixture = TestBed.createComponent(ClaimIntake);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
