import { cva } from "class-variance-authority";
import { BulletItem } from "./BulletItem";

const sectionVariants = cva("px-6 py-12", {
	variants: {
		background: {
			true: "bg-slate-800/30",
		},
	},
});

const textVariants = cva("lg:col-span-2", {
	variants: {
		reverse: {
			true: "order-1 lg:order-2",
		},
	},
});

const imageVariants = cva(
	"lg:col-span-3 rounded-xl overflow-hidden shadow-[0_20px_60px_-15px_rgba(0,0,0,0.5)] border border-white/10",
	{
		variants: {
			reverse: {
				true: "order-2 lg:order-1",
			},
		},
	},
);

interface SectionProps {
	title: string;
	description: string;
	bullets: string[];
	imageSrc: string;
	imageAlt: string;
	reverse?: boolean;
	background?: boolean;
}

export const Section = ({
	title,
	description,
	bullets,
	imageSrc,
	imageAlt,
	reverse,
	background,
}: SectionProps) => {
	return (
		<section className={sectionVariants({ background })}>
			<div className="max-w-7xl mx-auto">
				<div className="grid lg:grid-cols-5 gap-12 items-center">
					<div className={textVariants({ reverse })}>
						<h2 className="text-4xl text-white mb-6">{title}</h2>
						<p className="text-lg text-slate-300 mb-6">{description}</p>
						<ul className="space-y-3 text-slate-300">
							{bullets.map((bullet) => (
								<BulletItem key={bullet} text={bullet} />
							))}
						</ul>
					</div>
					<div className={imageVariants({ reverse })}>
						<img src={imageSrc} alt={imageAlt} className="w-full h-auto" />
					</div>
				</div>
			</div>
		</section>
	);
};
