/** @type {import('tailwindcss').Config} */
/* disable eslint errors if any */
// const defaultTheme = require('tailwindcss/defaultTheme');

export default {
  mode: 'jit',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Chakra Petch"'],
      },
      colors: {
        'page-black': '#121212',
        'card-black': '#100C1D',
        'card-light': '#141022',
        'card-border': '#1E192C',
        input: '#1E1A2D',
        primary: '#8502fb',
        'primary-light': '#9d1ffc',
        'primary-dark': '#8502fb42',
        'mint-green': '#09FFC4',
        'lime-green': '#40FA47',
        'error-dark': '#FF1D1D1A',
      },
      screens: {
        '2xl': '1700px',
      },
      backgroundImage: {
        'custom-gradient': 'radial-gradient(ellipse closest-side at 60% 250px, #42005E, #120A2A)',
      },
      transitionProperty: {
        'max-height': 'max-height',
      },
      keyframes: {
        slideDownAndFade: {
          from: { opacity: '0', transform: 'translateY(-2px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        slideLeftAndFade: {
          from: { opacity: '0', transform: 'translateX(2px)' },
          to: { opacity: '1', transform: 'translateX(0)' },
        },
        slideUpAndFade: {
          from: { opacity: '0', transform: 'translateY(2px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        slideRightAndFade: {
          from: { opacity: '0', transform: 'translateX(-2px)' },
          to: { opacity: '1', transform: 'translateX(0)' },
        },
      },
      animation: {
        slideDownAndFade: 'slideDownAndFade 400ms cubic-bezier(0.16, 1, 0.3, 1)',
        slideLeftAndFade: 'slideLeftAndFade 400ms cubic-bezier(0.16, 1, 0.3, 1)',
        slideUpAndFade: 'slideUpAndFade 400ms cubic-bezier(0.16, 1, 0.3, 1)',
        slideRightAndFade: 'slideRightAndFade 400ms cubic-bezier(0.16, 1, 0.3, 1)',
      },
    },
  },
  plugins: [require('tailwindcss-radix')()],
};
