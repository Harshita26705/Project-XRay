/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        risk: {
          red: '#dc2626',
          yellow: '#d97706',
          green: '#16a34a',
          unknown: '#6b7280'
        }
      }
    }
  },
  plugins: []
};
