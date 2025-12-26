import { Outlet } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import Header from './Header';

const PublicLayout = () => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen flex flex-col">
      <Header isPublic={true} />
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="bg-white border-t border-gray-200 py-6 mt-auto">
        <div className="container mx-auto px-4 text-center text-sm text-gray-600">
          <p>
            © {new Date().getFullYear()}{' '}
            <a 
              href={`https://${t('public.footer.website')}`} 
              target="_blank" 
              rel="noopener noreferrer"
              className="text-blue-600 hover:text-blue-800 underline"
            >
              X-Lab
            </a>
            {' '}Status Service
          </p>
        </div>
      </footer>
    </div>
  );
};

export default PublicLayout;

