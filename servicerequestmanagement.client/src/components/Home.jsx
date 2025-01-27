import React, { useContext, useEffect } from 'react';
import { AuthContext } from '../AuthContext';
//import Particles from 'react-tsparticles';
import { loadFull } from 'tsparticles';
import styles from './Home.module.css';

function Home() {
  const { isLoggedIn, userName, userRole } = useContext(AuthContext);

  useEffect(() => {
    document.body.style.overflow = 'hidden'; // Prevent scrolling during hero section
    return () => {
      document.body.style.overflow = 'auto'; // Restore scrolling after leaving page
    };
  }, []);

  return (
    <div className={styles.hero}>
      {/* Hero Section with Background Video */}
      <div className={styles.heroSection}>
        <iframe
          className={styles.backgroundVideo}
          src="https://www.youtube-nocookie.com/embed/qGQtTx9cIvs?start=20&autoplay=1&mute=1&loop=1&playlist=qGQtTx9cIvs&modestbranding=1&showinfo=0&rel=0&controls=0&disablekb=1"
          title="Background Video"
          frameBorder="0"
          allow="autoplay; fullscreen"
        ></iframe>
        <div className={styles.herodiv}>
          <div className={styles.heroContent}>
            <h1 className={styles.welcomeText}>Welcome to Service Management</h1>
            <p className={styles.description}>Manage your services efficiently with our platform.</p>
            {isLoggedIn && (
              <div>
                <h2 className={styles.userName}>Hello, {userName}!</h2>
                <p className={styles.userRole}>Your role: {userRole}</p>
              </div>
            )}
          </div>
        </div>
        
      </div>
    </div>
  );
}

export default Home;