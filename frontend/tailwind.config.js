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
        },
        xray: {
          bg: '#191f36',
          panel: '#262b40',
          main: '#00f0ff',
          accent: '#42a5f5'
        }
      },
      keyframes: {
        float: {
          '0%, 100%': { transform: 'translateY(0rem)' },
          '50%': { transform: 'translateY(-2rem)' }
        },
        floatCard: {
          '0%, 100%': { transform: 'translateY(0rem) translateX(0rem)' },
          '50%': { transform: 'translateY(-1.5rem) translateX(1rem)' }
        },
        fadeIn: {
          from: { opacity: 0, transform: 'translateY(1rem)' },
          to: { opacity: 1, transform: 'translateY(0)' }
        }
      },
      animation: {
        float: 'float 6s ease-in-out infinite',
        floatCard: 'floatCard 3s ease-in-out infinite',
        fadeIn: 'fadeIn 0.3s ease'
      }
    }
  },
  plugins: []
};
