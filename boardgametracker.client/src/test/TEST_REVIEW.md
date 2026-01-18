# Test Review - Action Items

## Summary Table

| # | Issue | Priority | Status | Files Affected |
|---|-------|----------|--------|----------------|
| 1 | Over-mocking - duplicate i18next mocks | High | ✅ Fixed | All test files |
| 2 | Testing CSS classes instead of behavior | High | ✅ Fixed | BgtDateTimePicker, BgtDatePicker |
| 3 | Weak assertions - testing mock data | Medium | ✅ Fixed | BgtDatePicker |
| 4 | Using `document.querySelector` instead of accessible queries | Medium | ✅ Fixed | BgtDateTimePicker |
| 5 | Inconsistent mock patterns | Low | ✅ Fixed | BgtPlayerSelector |
| 6 | Missing negative test cases | Medium | ✅ Fixed | All modal tests |

---

## Detailed Issues

### 1. Over-mocking kills test value ✅ FIXED

**Problem:** Every test file duplicates the same i18next mock.

**Solution:** Created shared test utilities:
- `src/test/setup.ts` - Global mocks for `react-i18next` and `i18next`
- `src/test/test-utils.tsx` - Shared `renderWithProviders` and `renderWithTheme` functions

**Files created/modified:**
- `src/test/setup.ts` - Added global i18n mocks
- `src/test/test-utils.tsx` - Created with render helpers and `createMockField`
- `src/routes/-modals/BgtDeleteModal.test.tsx` - Updated to use shared utilities

---

### 2. Testing implementation details, not behavior ✅ FIXED

**Problem:** Tests check for CSS classes like `flex`, `gap-2`, `w-32`

**Solution:** Removed all CSS class assertion tests from BgtDateTimePicker.test.tsx

**Removed tests:**
- `should have flex layout with gap`
- `should have time input with fixed width`
- `should show error styling on time input when there are errors`

---

### 3. Weak assertions - testing mock data ✅ FIXED

**Problem:** Asserting values on mock objects we just created

**Example:**
```typescript
expect(fieldWithValue.state.value).toBe('2024-06-15'); // Testing our own mock!
```

**Solution:** Test actual rendered output instead - verify placeholder disappears when date is selected.

**Files modified:**
- `src/components/BgtForm/BgtDatePicker.test.tsx` - Changed to test `queryByText('Pick a date')` is not in document

---

### 4. Using `document.querySelector` ✅ FIXED

**Problem:** Direct DOM queries bypass accessibility

**Example:**
```typescript
const timeInput = document.querySelector('input[type="time"]');
```

**Solution:** Use `screen.getByPlaceholderText('xx:xx')` and `screen.getByDisplayValue()` for time inputs (time inputs have no implicit ARIA role).

**Files modified:**
- `src/components/BgtForm/BgtDateTimePicker.test.tsx` - Replaced all `document.querySelector` calls

---

### 5. Inconsistent mock patterns ✅ FIXED

**Problem:** BgtPlayerSelector mocks `i18next` while others mock `react-i18next`

**Solution:** Both are now mocked globally in `setup.ts`

---

### 6. Missing negative test cases ✅ FIXED

**Problem:** Only happy paths tested

**Added tests:**
- Form submission with validation errors (empty name field)
- BgtDeleteModal async error handling

**Files modified:**
- `src/routes/locations/-modals/NewLocationModal.test.tsx` - Added validation error test
- `src/routes/locations/-modals/EditLocationModal.test.tsx` - Added validation error test
- `src/routes/-modals/BgtDeleteModal.test.tsx` - Added async error handling tests

---

## Progress Log

- [x] Issue 1: Create shared test utilities
- [x] Issue 2: Remove CSS class assertions
- [x] Issue 3: Fix weak assertions
- [x] Issue 4: Replace document.querySelector
- [x] Issue 5: Standardize mock patterns
- [x] Issue 6: Add negative test cases
