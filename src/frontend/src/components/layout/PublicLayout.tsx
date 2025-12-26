import { Outlet } from 'react-router-dom';
import Header from './Header';

const PublicLayout = () => {
  return (
    <div className="min-h-screen flex flex-col">
      <Header isPublic={true} />
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="bg-white border-t border-gray-200 py-6 mt-auto">
        <div className="container mx-auto px-4 text-center text-sm text-gray-600">
          <p>&copy; {new Date().getFullYear()} X-Lab Status Service</p>
          <p className="mt-1">
            <a href="https://x-lab.by" target="_blank" rel="noopener noreferrer">
              x-lab.by
            </a>
          </p>
        </div>
      </footer>
    </div>
  );
};

export default PublicLayout;

