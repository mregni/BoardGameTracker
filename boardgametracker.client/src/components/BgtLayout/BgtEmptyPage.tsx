import type { ComponentType, PropsWithChildren, SVGProps } from "react";
import { BgtEmptyState } from "./BgtEmptyState";
import { BgtPage } from "./BgtPage";
import { BgtPageContent } from "./BgtPageContent";
import BgtPageHeader from "./BgtPageHeader";

interface Props extends PropsWithChildren {
	header: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
	emptyIcon?: ComponentType<SVGProps<SVGSVGElement>>;
	title: string;
	description: string;
	action?: {
		label: string;
		onClick: () => void;
	};
}

export const BgtEmptyPage = (props: Props) => {
	const { header, icon, emptyIcon, title, description, action, children } = props;

	return (
		<BgtPage>
			<BgtPageHeader header={header} icon={icon} />
			<BgtPageContent centered>
				<BgtEmptyState icon={emptyIcon ?? icon} title={title} description={description} action={action} />
				{children}
			</BgtPageContent>
		</BgtPage>
	);
};
