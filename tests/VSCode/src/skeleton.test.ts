import * as assert from 'node:assert';

describe('Reqnroll VS Code Test Skeleton', () => {
  it('should compile and run a basic test', () => {
    assert.strictEqual(1 + 1, 2);
  });

  it('should discover all test files', () => {
    // This test exists purely to validate that the test project
    // scaffolding is wired correctly: TypeScript compilation, Mocha
    // discovery, and execution all work.
    assert.ok(true, 'Test skeleton is operational');
  });
});
