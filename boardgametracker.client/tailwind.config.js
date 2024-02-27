/** @type {import('tailwindcss').Config} */
export default {
  content: ["**/*.{ts,tsx}"],
  theme: {
    
  },
  plugins: [require("tailwindcss-radix")()],
}

