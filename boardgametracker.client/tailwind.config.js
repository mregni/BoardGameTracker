/** @type {import('tailwindcss').Config} */
/* disable eslint errors if any */
const defaultTheme = require('tailwindcss/defaultTheme');

export default {
  mode: 'jit',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Chakra Petch"', ...defaultTheme.fontFamily.sans],
      },
      colors: {
        'page-black': '#121212',
        'card-black': '#100C1D',
        'card-light': '#141022',
        'card-border': '#1E192C',
        primary: '#8502fb',
        'primary-dark': '#8502fb42',
        'mint-green': '#09FFC4',
        'lime-green': '#40FA47',
      },
      screens: {
        '2xl': '1700px',
      },
      backgroundImage: {
        'custom-gradient': 'radial-gradient(ellipse closest-side at 40% 60%, #2E004C, #060214)',
      },
      transitionProperty: {
        'max-height': 'max-height',
      },
    },
  },
  plugins: [require('tailwindcss-radix')()],
};
