/**
 * Runner sanity check — proves the Vitest unit-test runner is wired and exits
 * green on an empty (pre-feature) suite. Real feature specs land in
 * T019 / T028 / T038 / T047 / T050.
 */
describe('Test runner sanity', () => {
  it('executes a passing assertion', () => {
    expect(true).toBe(true);
  });
});
