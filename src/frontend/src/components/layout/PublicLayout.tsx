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
          <p>{t('public.footer.copyright', { year: new Date().getFullYear() })}</p>
          <p className="mt-1">
            <a href="https://x-lab.by" target="_blank" rel="noopener noreferrer">
              {t('public.footer.website')}
            </a>
          </p>
        </div>
      </footer>
    </div>
  );
};

export default PublicLayout;

