/** @type {import('tailwindcss').Config} */
/* disable eslint errors if any */
const defaultTheme = require('tailwindcss/defaultTheme');

export default {
  mode: 'jit',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        'chakra-petch': ['Chakra Petch', ...defaultTheme.fontFamily.sans],
      },
      colors: {
        'page-black': '#121212',
        'card-black': '#1D1D1D',
        primary: '#8502fb',
        'primary-dark': '#8502fb42',
      },
      screens: {
        '2xl': '1700px',
      },
      backgroundImage: {
        'custom-gradient': 'radial-gradient(ellipse closest-side at 40% 60%, #8502fb, #060214)',
      },
    },
  },
  plugins: [require('tailwindcss-radix')()],
};
