import js from '@eslint/js'
import reactHooks from 'eslint-plugin-react-hooks'
import globals from 'globals'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  {
    ignores: ['dist/**', 'dist-ssr/**'],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  reactHooks.configs.flat.recommended,
  {
    files: ['src/**/*.{js,jsx,ts,tsx}'],
    languageOptions: {
      globals: globals.browser,
    },
  },
  {
    files: ['*.config.{js,ts}', 'eslint.config.js'],
    languageOptions: {
      globals: globals.node,
    },
  },
)
