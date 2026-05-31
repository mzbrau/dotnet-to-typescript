import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Assembly-driven typing',
    description: (
      <>
        Generate first-class TypeScript definitions directly from your compiled C# assemblies,
        so scripts always match your runtime contracts.
      </>
    ),
  },
  {
    title: 'Practical CLI workflow',
    description: (
      <>
        Use a simple command to process one or many assemblies, choose output paths, and keep
        generated files aligned with every release.
      </>
    ),
  },
  {
    title: 'Ready for script engines',
    description: (
      <>
        Produce declaration files and instance stubs that work smoothly for IntelliSense-heavy
        scripting scenarios such as Jint-based integrations.
      </>
    ),
  },
];

function Feature({title, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4', styles.featureCard)}>
      <div className={styles.featureContent}>
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props) => (
            <Feature key={props.title} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
