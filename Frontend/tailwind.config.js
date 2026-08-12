/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#0064d2',
          50: '#eff6ff',
          100: '#dbeafe',
          500: '#0064d2',
          600: '#0055b3',
          700: '#004494',
        },
        secondary: {
          DEFAULT: '#e53238',
          500: '#e53238',
          600: '#c41e24',
        },
        accent: {
          DEFAULT: '#f5af02',
          500: '#f5af02',
        },
        success: '#86b817',
        shopease: {
          blue: '#0064d2',
          red: '#e53238',
          yellow: '#f5af02',
          green: '#86b817',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      screens: {
        xs: '480px',
      },
    },
  },
  plugins: [],
}
