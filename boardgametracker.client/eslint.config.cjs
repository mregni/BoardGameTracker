const { defineConfig, globalIgnores } = require('eslint/config');

const globals = require('globals');

const { fixupConfigRules, fixupPluginRules } = require('@eslint/compat');

const tsParser = require('@typescript-eslint/parser');
const reactRefresh = require('eslint-plugin-react-refresh');
const _import = require('eslint-plugin-import');
const autofix = require('eslint-plugin-autofix');
const tanstackQuery = require('@tanstack/eslint-plugin-query');
const js = require('@eslint/js');

const { FlatCompat } = require('@eslint/eslintrc');

const compat = new FlatCompat({
  baseDirectory: __dirname,
  recommendedConfig: js.configs.recommended,
  allConfig: js.configs.all,
});

module.exports = defineConfig([
  {
    settings: {
      react: {
        version: 'detect',
      },
    },

    languageOptions: {
      globals: {
        ...globals.browser,
      },

      parser: tsParser,
      ecmaVersion: 'latest',
      sourceType: 'module',

      parserOptions: {
        project: ['./tsconfig.json', './tsconfig.node.json'],
        tsconfigRootDir: __dirname,
      },
    },

    extends: fixupConfigRules(
      compat.extends(
        'eslint:recommended',
        'plugin:@typescript-eslint/recommended',
        'plugin:react-hooks/recommended',
        'plugin:react/recommended',
        'plugin:react/jsx-runtime'
      )
    ),

    plugins: {
      'react-refresh': reactRefresh,
      import: fixupPluginRules(_import),
      autofix,
      '@tanstack/query': tanstackQuery,
    },

    rules: {
      'no-plusplus': 'error',
      'no-console': 'error',
      'autofix/no-debugger': 'error',

      'react-refresh/only-export-components': [
        'warn',
        {
          allowConstantExport: true,
        },
      ],

      'import/order': [
        'warn',
        {
          'newlines-between': 'always',

          alphabetize: {
            order: 'desc',
            caseInsensitive: true,
          },
        },
      ],

      '@tanstack/query/exhaustive-deps': 'error',
      '@tanstack/query/no-rest-destructuring': 'warn',
      '@tanstack/query/stable-query-client': 'error',
    },
  },
  globalIgnores(['**/dist', '**/.eslintrc.cjs', '**/*.config.js', '**/*.config.ts', '**/*.config.cjs']),
  {
    files: ['**/models/index.ts'],

    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
]);
