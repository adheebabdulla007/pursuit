import { Search } from 'lucide-react'
import { useState } from 'react'
import { Button } from '../ui/Button'
import { Input } from '../ui/Input'

interface JobSearchBarProps {
  initialKeyword: string
  initialLocation: string
  onSubmit: (keyword: string, location: string) => void
}

export function JobSearchBar({
  initialKeyword,
  initialLocation,
  onSubmit,
}: JobSearchBarProps) {
  const [keyword, setKeyword] = useState(initialKeyword)
  const [location, setLocation] = useState(initialLocation)

  return (
    <form
      aria-label="Search jobs"
      className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.8fr)_auto]"
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit(keyword, location)
      }}
    >
      <Input
        id="keyword"
        label="Keyword"
        value={keyword}
        onChange={(event) => setKeyword(event.target.value)}
      />
      <Input
        id="location"
        label="Location"
        value={location}
        onChange={(event) => setLocation(event.target.value)}
      />
      <Button type="submit" className="h-12 lg:h-auto">
        <Search aria-hidden="true" size={17} />
        Search jobs
      </Button>
    </form>
  )
}
