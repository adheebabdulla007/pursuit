type JobCardProps = {
  title: string
  companyName: string
  location: string
}

function JobCard({ title, companyName, location }: JobCardProps) {
  return (
    <div>
      <h1>{title}</h1>
      <p>{companyName} | {location}</p>
    </div>
  )
}

export default JobCard