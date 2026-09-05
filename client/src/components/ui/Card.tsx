import { forwardRef, type HTMLAttributes } from 'react';

type CardPadding = 'none' | 'sm' | 'md';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  padding?: CardPadding;
}

const paddingClasses: Record<CardPadding, string> = {
  none: '',
  sm: 'p-4',
  md: 'p-6',
};

export const Card = forwardRef<HTMLDivElement, CardProps>(
  ({ padding = 'md', className = '', children, ...props }, ref) => {
    const classes = [
      'bg-white rounded-lg shadow border border-neutral-200',
      paddingClasses[padding],
      className,
    ]
      .filter(Boolean)
      .join(' ');

    return (
      <div ref={ref} className={classes} {...props}>
        {children}
      </div>
    );
  }
);

Card.displayName = 'Card';